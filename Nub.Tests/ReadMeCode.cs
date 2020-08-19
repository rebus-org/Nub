using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleNamedExpression

namespace Nub.Tests
{
    [TestFixture]
    public class ReadMeCode
    {
        [Test]
        public void METHOD()
        {
            var masterDataConnectionString = "";
            var projectionDatabaseConnectionString = "";

            var services = new ServiceCollection();

            services.AddSingletonWithKey("masterdata", p => GetMongoDatabase(masterDataConnectionString));
            services.AddSingletonWithKey("projections", p => GetMongoDatabase(projectionDatabaseConnectionString));

            services.DecorateKeyed<IMongoDatabase>((p, database) => new MongoDatabaseDecorator(database: database));
            services.DecorateKeyed<IMongoDatabase>("masterdata", (p, database) => new MongoDatabaseDecorator(database: database));

            services.AddTransient(p => new SomeKindOfProjection(database: p.GetServiceByKey<IMongoDatabase>("projections")));
        }

        IMongoDatabase GetMongoDatabase(string connectionString)
        {
            throw new System.NotImplementedException();
        }

        interface IMongoDatabase { }

        class SomeKindOfProjection
        {
            public SomeKindOfProjection(IMongoDatabase database)
            {
            }
        }

        class MongoDatabaseDecorator : IMongoDatabase
        {
            public MongoDatabaseDecorator(IMongoDatabase database)
            {

            }
        }
    }
}