# Nub

Extensions that enable registering services with a key in Microsoft's DI container.

## When would you need it?

E.g. when registering multiple `IMongoDatabase` instances in your container. Then you would go

```csharp
services.AddSingletonWithKey("masterdata", p => GetMongoDatabase(masterDataConnectionString));
services.AddSingletonWithKey("projections", p => GetMongoDatabase(projectionDatabaseConnectionString));
```

to register them under the keys "masterdata" and "projections" respectively, and then when it's time to resolve them, 
you would do it by registering a factory like so:

```csharp
services.AddTransient(p => new SomeKindOfProjection(database: p.GetServiceByKey<IMongoDatabase>("projections")));
```

Great!

## So what about decorators?

You can decorate your "keyed registrations", so e.g. to have all `IMongoDatabase` instances wrapped in something, you
would go

```csharp
services.DecorateKeyed<IMongoDatabase>((p, database) => new MongoDatabaseDecorator(database: database));
```

to have it decorated. You could also target a specific keyed registration by specifying a key:

```csharp
services.DecorateKeyed<IMongoDatabase>("masterdata", (p, database) => new MongoDatabaseDecorator(database: database));
```

and that's basically it!

---

BTW the ability to DECORATE things is provided by means of the immensely useful [Scrutor](https://github.com/khellang/Scrutor) library 🧨🔥👴
