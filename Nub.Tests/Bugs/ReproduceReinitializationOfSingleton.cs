using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Nub.Tests.Bugs
{
    [TestFixture]
    public class ReproduceReinitializationOfSingleton
    {
        [Test]
        public void DoesNotBehaveAsIfItWasTransient()
        {
            var services = new ServiceCollection();

            services.AddSingletonWithKey("thing", p => new Thing());

            using var provider = services.BuildServiceProvider();

            provider.GetServiceByKey<Thing>("thing");
            provider.GetServiceByKey<Thing>("thing");
            provider.GetServiceByKey<Thing>("thing");

            Assert.That(Thing.InstanceCounter, Is.EqualTo(1));
        }

        class Thing
        {
            public static int InstanceCounter;

            public Thing() => Interlocked.Increment(ref InstanceCounter);
        }
    }
}