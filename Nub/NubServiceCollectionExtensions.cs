using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable SimplifyLinqExpressionUseAll
// ReSharper disable SimplifyLinqExpression

namespace Nub
{
    public static class NubServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a singleton of type <typeparamref name="TService"/> with the key given by <paramref name="key"/>. Uses the factory <paramref name="factory"/> to resolve the instance
        /// </summary>
        public static IServiceCollection AddSingletonWithKey<TService>(this IServiceCollection services, string key, Func<IServiceProvider, TService> factory)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

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
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var holder = provider.GetService<KeyedServiceHolder<TService>>();

            if (holder == null)
            {
                throw new ArgumentException($"Cannot find keyed service holder for services of type {typeof(TService)} - tried to get keyed service with key '{key}', but it doesn't look like any keyed services were registered");
            }

            return holder.Get(key, provider);
        }

        /// <summary>
        /// Adds a decorator for all services of type <typeparamref name="TService"/> registered with a key. Uses the decorator function <paramref name="decorator"/> to decorate the
        /// returned instances.
        /// </summary>
        public static IServiceCollection DecorateKeyed<TService>(this IServiceCollection services, Func<IServiceProvider, TService, TService> decorator)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (decorator == null) throw new ArgumentNullException(nameof(decorator));

            services.Decorate<KeyedServiceHolder<TService>>((holder, provider) =>
            {
                holder.Decorate(decorator);
                return holder;
            });

            return services;
        }

        /// <summary>
        /// Adds a decorator for service of type <typeparamref name="TService"/> and the key given by <paramref name="key"/>. Uses the decorator function <paramref name="decorator"/> to decorate the
        /// returned instance.
        /// </summary>
        public static IServiceCollection DecorateKeyed<TService>(this IServiceCollection services, string key, Func<IServiceProvider, TService, TService> decorator)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (decorator == null) throw new ArgumentNullException(nameof(decorator));

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
            readonly ConcurrentDictionary<string, T> _instances = new ConcurrentDictionary<string, T>();
            readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();

            public void Add(string key, Func<IServiceProvider, T> factory)
            {
                if (key == null) throw new ArgumentNullException(nameof(key));
                if (factory == null) throw new ArgumentNullException(nameof(factory));

                if (_factories.ContainsKey(key)) throw new ArgumentException($"Cannot add new factory for {typeof(T)} with key '{key}', because a factory with that key exists already!");

                _factories[key] = new Lazy<Func<IServiceProvider, T>>(() => factory);
            }

            public T Get(string key, IServiceProvider provider)
            {
                if (key == null) throw new ArgumentNullException(nameof(key));
                if (provider == null) throw new ArgumentNullException(nameof(provider));

                return _instances.GetOrAdd(key, _ => _factories.TryGetValue(key, out var lazy)
                    ? GetLazyValue(provider, lazy)
                    : throw new ArgumentException($"Could not find a registered instance of {typeof(T)} with key '{key}'"));
            }

            public void Decorate(Func<IServiceProvider, T, T> decorator)
            {
                if (decorator == null) throw new ArgumentNullException(nameof(decorator));

                foreach (var factory in _factories)
                {
                    var key = factory.Key;
                    var lazy = factory.Value;

                    _factories[key] = new Lazy<Func<IServiceProvider, T>>(() => provider => decorator(provider, GetLazyValue(provider, lazy)));
                }
            }

            public void Decorate(string key, Func<IServiceProvider, T, T> decorator)
            {
                if (key == null) throw new ArgumentNullException(nameof(key));
                if (decorator == null) throw new ArgumentNullException(nameof(decorator));

                var lazy = _factories.TryGetValue(key, out var result)
                    ? result
                    : throw new InvalidOperationException($"Tried to add decorator for {typeof(T)}, but no lazy factory could be found for key {key}");

                _factories[key] = new Lazy<Func<IServiceProvider, T>>(() => provider => decorator(provider, GetLazyValue(provider, lazy)));
            }

            T GetLazyValue(IServiceProvider provider, Lazy<Func<IServiceProvider, T>> lazy)
            {
                if (provider == null) throw new ArgumentNullException(nameof(provider));
                if (lazy == null) throw new ArgumentNullException(nameof(lazy));

                var value = lazy.Value(provider);

                if (value is IDisposable disposable)
                {
                    _disposables.Push(disposable);
                }

                return value;
            }

            public void Dispose()
            {
                while (_disposables.TryPop(out var disposable))
                {
                    disposable.Dispose();
                }
            }
        }
    }
}