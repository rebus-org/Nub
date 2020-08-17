using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testy;

namespace Nub.Tests
{
    [TestFixture]
    public class TestRegistrationOfDisposableThings : FixtureBase
    {
        [Test]
        public void KeyedInstanceGetsDisposed()
        {
            var services = new ServiceCollection();

            var events = new ConcurrentQueue<string>();
            services.AddSingleton(events);
            services.AddSingletonWithKey<IMyService>("disposable", p => new MyDisposableService(p.GetRequiredService<ConcurrentQueue<string>>()));

            var provider = Using(services.BuildServiceProvider());

            var instance = provider.GetServiceByKey<IMyService>("disposable");
            Assert.That(instance, Is.TypeOf<MyDisposableService>());

            Assert.That(events.DequeueAll(), Is.EqualTo(new string[0]));

            CleanUpDisposables();

            Assert.That(events.DequeueAll(), Is.EqualTo(new[] { "disposed 🙂" }));
        }

        [Test]
        public void EvenTheMostDecoratedDisposableGetsDisposed()
        {
            var services = new ServiceCollection();

            var events = new ConcurrentQueue<string>();
            services.AddSingleton(events);
            services.AddSingletonWithKey<IMyService>("disposable", p => new MyDisposableService(p.GetRequiredService<ConcurrentQueue<string>>()));
            services.DecorateKeyed<IMyService>("disposable", (p, decoratee) => new MyServiceDecorator(decoratee));

            var provider = Using(services.BuildServiceProvider());

            var instance = provider.GetServiceByKey<IMyService>("disposable");
            Assert.That(instance, Is.TypeOf<MyServiceDecorator>());

            var decorator = (MyServiceDecorator)instance;
            Assert.That(decorator.Decoratee, Is.TypeOf<MyDisposableService>());

            Assert.That(events.DequeueAll(), Is.EqualTo(new string[0]));

            CleanUpDisposables();

            Assert.That(events.DequeueAll(), Is.EqualTo(new[] { "disposed 🙂" }));
        }

        interface IMyService
        {
        }

        class MyServiceDecorator : IMyService
        {
            public IMyService Decoratee { get; }

            public MyServiceDecorator(IMyService decoratee) => Decoratee = decoratee;
        }

        class MyDisposableService : IMyService, IDisposable
        {
            readonly ConcurrentQueue<string> _events;

            public MyDisposableService(ConcurrentQueue<string> events) => _events = events;

            public void Dispose() => _events.Enqueue("disposed 🙂");
        }
    }

}