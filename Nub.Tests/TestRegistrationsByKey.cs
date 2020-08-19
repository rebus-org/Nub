using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Nub.Tests
{
    [TestFixture]
    public class TestRegistrationsByKey
    {
        [Test]
        public void CanDecorateServiceRegisteredWithKey()
        {
            var services = new ServiceCollection();

            services.AddSingletonWithKey("blub", p => "blub");
            services.AddSingletonWithKey("bløb", p => "bløb");

            services.DecorateKeyed<string>("blub", (p, str) => $"DECORATED{str}");

            using var provider = services.BuildServiceProvider();

            var blub = provider.GetServiceByKey<string>("blub");
            var bløb = provider.GetServiceByKey<string>("bløb");

            Assert.That(blub, Is.EqualTo("DECORATEDblub"));
            Assert.That(bløb, Is.EqualTo("bløb"));
        }

        [Test]
        public void CanDecorateAllServicesRegisteredWithKey()
        {
            var services = new ServiceCollection();

            services.AddSingletonWithKey("blub", p => "blub");
            services.AddSingletonWithKey("bløb", p => "bløb");

            services.DecorateKeyed<string>((p, str) => $"DECORATED{str}");

            using var provider = services.BuildServiceProvider();

            var blub = provider.GetServiceByKey<string>("blub");
            var bløb = provider.GetServiceByKey<string>("bløb");

            Assert.That(blub, Is.EqualTo("DECORATEDblub"));
            Assert.That(bløb, Is.EqualTo("DECORATEDbløb"));
        }

        [Test]
        public void CanHandleKeyedThings()
        {
            var services = new ServiceCollection();

            var a = new Something();
            var b = new Something();
            var c = new Something();

            services
                .AddSingletonWithKey("a", p => a)
                .AddSingletonWithKey("b", p => b)
                .AddSingletonWithKey("c", p => c);

            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetServiceByKey<Something>("a"), Is.SameAs(a));
            Assert.That(provider.GetServiceByKey<Something>("b"), Is.SameAs(b));
            Assert.That(provider.GetServiceByKey<Something>("c"), Is.SameAs(c));
        }

        [Test]
        public void CanUseServiceProviderForFurtherLookups()
        {
            var services = new ServiceCollection();

            services.AddSingletonWithKey("a", provider =>
            {
                if (provider == null) throw new ArgumentNullException(nameof(provider), "Service provider instance was NULL!");

                return new Something();
            });

            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetServiceByKey<Something>("a"), Is.TypeOf<Something>());
        }

        class Something
        {
        }
    }
}