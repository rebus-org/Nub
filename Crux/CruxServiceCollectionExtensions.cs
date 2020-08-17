using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable SimplifyLinqExpressionUseAll

namespace Crux
{
    public static class CruxServiceCollectionExtensions
    {
        public static IServiceCollection DecorateKeyed<T>(this IServiceCollection services, string key, Func<IServiceProvider, T, T> decorator)
        {
            if (!services.Any(s => s.ServiceType == typeof(KeyedServiceHolder<T>)))
            {
                throw new InvalidOperationException($"Cannot decorate {typeof(T)} service with key {key}, because it has not been registered");
            }

            services.Decorate<KeyedServiceHolder<T>>((holder, provider) =>
            {
                holder.Decorate(key, decorator);
                return holder;
            });

            return services;
        }

        public static IServiceCollection AddSingletonWithKey<T>(this IServiceCollection services, string key, Func<IServiceProvider, T> factory)
        {
            if (!services.Any(s => s.ServiceType == typeof(KeyedServiceHolder<T>)))
            {
                services.AddSingleton(new KeyedServiceHolder<T>());
            }

            services.Decorate<KeyedServiceHolder<T>>((holder, provider) =>
            {
                holder.Add(key, factory);
                return holder;
            });

            return services;
        }

        public static T GetServiceByKey<T>(this IServiceProvider provider, string key)
        {
            var holder = provider.GetService<KeyedServiceHolder<T>>();

            if (holder == null)
            {
                throw new ArgumentException($"Cannot find keyed service holder for services of type {typeof(T)} - tried to get keyed service with key '{key}', but it doesn't look like any keyed services were registered");
            }

            return holder.Get(key, provider);
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
