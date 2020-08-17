using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Crux
{
    public static class CruxServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a singleton of type <typeparamref name="TService"/> with the key given by <paramref name="key"/>. Uses the factory <paramref name="factory"/> to resolve the instance
        /// </summary>
        public static IServiceCollection AddSingletonWithKey<TService>(this IServiceCollection services, string key, Func<IServiceProvider, TService> factory)
        {
            if (!services.Any(s => s.ServiceType == typeof(KeyedServiceHolder<TService>)))
            {
                services.AddSingleton(new KeyedServiceHolder<TService>());
            }

            services.Decorate<KeyedServiceHolder<TService>>((holder, provider) =>
            {
                holder.Add(key, factory);
                return holder;
            });

            return services;
        }

        /// <summary>
        /// Gets the instance of type <typeparamref name="TService"/> with the key given by <paramref name="key"/>. Throws an <see cref="ArgumentException"/> if
        /// no matching registration could be found
        /// </summary>
        public static TService GetServiceByKey<TService>(this IServiceProvider provider, string key)
        {
            var holder = provider.GetService<KeyedServiceHolder<TService>>();

            if (holder == null)
            {
                throw new ArgumentException($"Cannot find keyed service holder for services of type {typeof(TService)} - tried to get keyed service with key '{key}', but it doesn't look like any keyed services were registered");
            }

            return holder.Get(key, provider);
        }

        /// <summary>
        /// Adds a decorator for service of type <typeparamref name="TService"/> and the key given by <paramref name="key"/>. Uses the decorator function <paramref name="decorator"/> to decorate the
        /// returned instance.
        /// </summary>
        public static IServiceCollection DecorateKeyed<TService>(this IServiceCollection services, string key, Func<IServiceProvider, TService, TService> decorator)
        {
            if (!services.Any(s => s.ServiceType == typeof(KeyedServiceHolder<TService>)))
            {
                throw new InvalidOperationException($"Cannot decorate {typeof(TService)} service with key {key}, because it has not been registered");
            }

            services.Decorate<KeyedServiceHolder<TService>>((holder, provider) =>
            {
                holder.Decorate(key, decorator);
                return holder;
            });

            return services;
        }

        class KeyedServiceHolder<T> : IDisposable
        {
            readonly ConcurrentDictionary<string, Lazy<Func<IServiceProvider, T>>> _factories = new ConcurrentDictionary<string, Lazy<Func<IServiceProvider, T>>>();

            public void Add(string key, Func<IServiceProvider, T> factory)
            {
                if (_factories.ContainsKey(key)) throw new ArgumentException($"Cannot add new factory for {typeof(T)} with key '{key}', because a factory with that key exists already!");

                _factories[key] = new Lazy<Func<IServiceProvider, T>>(() => factory);
            }

            public T Get(string key, IServiceProvider provider)
            {
                return _factories.TryGetValue(key, out var lazy)
                    ? lazy.Value(provider)
                    : throw new ArgumentException($"Could not find a registered instance of {typeof(T)} with key '{key}'");
            }

            public void Decorate(string key, Func<IServiceProvider, T, T> decorator)
            {
                var existing = _factories.TryGetValue(key, out var lazy)
                    ? lazy
                    : throw new InvalidOperationException($"Tried to add decorator for {typeof(T)}, but no lazy factory could be found for key {key}");

                _factories[key] = new Lazy<Func<IServiceProvider, T>>(() => provider => decorator(provider, existing.Value(provider)));
            }

            public void Dispose()
            {
                foreach (var factory in _factories.Values)
                {
                    if (!factory.IsValueCreated) continue;

                    if (factory.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

    }
}
