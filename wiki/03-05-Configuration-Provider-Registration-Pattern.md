# Configuration Provider Registration Pattern

All references in older docs to 1-arg or 2-arg
`RegisterRequiredServices(IServiceCollection)` / `(IServiceCollection, ILoggerFactory?)`
overloads, to the `RegisterOverrides` class, and to `Parent*` named arguments on
`[ServiceTypeCollection]` describe state that has been removed. Only the 5-arg form
below is supported.

## Principle

**Each ServiceTypeOption registers its own requirements.** The Configure / Register /
Initialize three-phase lifecycle is the only registration path. There is no separate
`AddFramework<Domain>()` extension and no `RegisterOverrides` registry — the package
reference + the `[TypeOption]` attribute + the module-initialiser emitted by
`Registration.SourceGenerators` together constitute the registration intent.

## The three phases

| Phase | When | What it does |
|---|---|---|
| `Configure(builder, loggerFactory)` | Before Build, after `AddConfigurationGateway`. | Binds `IOptions<List<TConfig>>` from `IConfiguration.GetSection("<Category>s")` (e.g. `"Connections"`) and calls each option's own `Configure`. |
| `Register(services, loggerFactory)` | Before Build, immediately after `Configure`. | Calls each option's `RegisterRequiredServices(...)` so per-type factories, providers, and decorators land in DI. |
| `Initialize(serviceProvider, loggerFactory)` | After Build, before middleware. | Materialises singletons that need a resolved provider (e.g. caches that prefetch). |

`Configure` and `Register` happen on the unbuilt `builder` / `services`. `Initialize` runs on
the built `app.Services`.

> **Configure binds from `IConfiguration`, not the gateway.** The generated `Configure` reads
> `configuration.GetSection("<Category>s")` to bind `IOptions<List<TConfig>>`. It does NOT read
> through `IConfigurationGateway`. Runtime records are read later, by the domain provider, through
> the gateway.

### The two generated `Configure` overloads

The `ServiceTypeCollectionGenerator` emits two `Configure` signatures:

```csharp
// Builder form — used in Program.cs. Binds GetSection("<Category>s") from builder.Configuration.
public static TBuilder Configure<TBuilder>(TBuilder builder, ILoggerFactory? loggerFactory = null)
    where TBuilder : IHostApplicationBuilder;

// Service-collection form — binds GetSection("<Category>s") AND calls each option's
// RegisterRequiredServices(services, loggerFactory, dataStoreName, pathName, containerName).
public static void Configure(
    IServiceCollection services,
    IConfiguration configuration,
    ILoggerFactory? loggerFactory,
    string dataStoreName,
    string pathName);
```

`Register(IServiceCollection, ILoggerFactory? = null)` and
`Initialize(IServiceProvider, ILoggerFactory? = null)` round out the generated surface.

## The canonical RegisterRequiredServices signature

Config providers read runtime records through `IConfigurationGateway`, so the provider takes a
`Lazy<IConfigurationGateway>` (deferred so gateway resolution happens on first use, not at
registration). The real `MsSqlConnectionType` does exactly this:

```csharp
public sealed class MyConnectionType : ConnectionTypeBase
{
    public override IServiceCollection RegisterRequiredServices(
        IServiceCollection services,
        ILoggerFactory? loggerFactory,
        string dataStoreName,
        string pathName,
        string containerName)
    {
        services.AddSingleton<IMyConnectionFactory, MyConnectionFactory>();
        services.TryAddSingleton<MyConnectionConfigurationProvider>(sp =>
            new MyConnectionConfigurationProvider(
                sp.GetRequiredService<IOptionsMonitor<List<MyConnectionConfiguration>>>(),
                sp.GetService<ILogger<MyConnectionConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName,
                pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>()),
                sp.GetRequiredService<Lazy<IReadOnlyList<IDataStore>>>()));
        return services;
    }
}
```

The five arguments are non-optional. The TypeCollection passes them in from its
`[ServiceTypeCollection]` metadata (`dataStoreName`, `pathName`, and each option's
`DefaultContainerName`) so every option lands its services against the correct path inside the
correct DataStore.

## Domain provider registration

After `Configure` and `Register`, the domain wires its provider into ConfigurationDb's specific
path via a static `RegisterDomainServices` on the per-domain provider (not on the
TypeCollection). It registers the `*ConfigurationProvider`, its cache, and any writer-side
plumbing, and returns `void` (it is not chainable).

**The signature varies per domain.** Most domains take `(services, dataStoreName, pathName)`:

```csharp
SchedulerTypes.Configure(builder, loggerFactory);
SchedulerTypes.Register(builder.Services, loggerFactory);
SchedulerConfigurationProvider.RegisterDomainServices(
    builder.Services,
    dataStoreName: "ConfigurationDb",
    pathName:      "sched");
```

`SchedulerConfigurationProvider.RegisterDomainServices` is also called from
`DefaultSchedulerType` during scheduler registration, and `CalculationConfigurationProvider` /
`SettingsConfigurationProvider` / `EscalationConfigurationProvider` are called from
`ServiceTypeExtensions`.

Several domains instead take an extra `IConfiguration` argument —
**Quality, Settings, Schedule, Notification, Escalation**:

```csharp
SettingsConfigurationProvider.RegisterDomainServices(
    builder.Services, builder.Configuration, "ConfigurationDb");

ScheduleConfigurationProvider.RegisterDomainServices(
    builder.Services, builder.Configuration, "ConfigurationDb", "sched");

QualityConfigurationProvider.RegisterDomainServices(
    builder.Services, builder.Configuration, "ConfigurationDb");
```

Check the specific provider before calling it — there is no uniform signature.

## Reading and invalidating through the provider

Domain providers expose a read API of `Get(name)`, `Get(id)`, and `Get()` (all
`Task<IGenericResult<…>>` on `IFdwServiceProvider`) — never `GetAll` / `List` / `Fetch`. Cache
invalidation is `ICacheInvalidator.InvalidateByTag`.

## Initialize phase

The entry-point app runs a single `PlatformServices.Initialize(app.Services, loggerFactory)` discovery
pass after Build; it invokes each domain's own `Initialize` in dependency-safe **Group** order — the
per-domain `SecretManagerTypes.Initialize` / `ConnectionTypes.Initialize` /
`ConfigurationGatewayDataStoreProvider.Initialize` / `DataSetProvider.Initialize` calls are no longer
hand-written:

```csharp
var app = builder.Build();

PlatformServices.Initialize(app.Services, loggerFactory);
```

Only collections that need a resolved `IServiceProvider` implement a non-trivial `Initialize`;
stateless factories declare a no-op to satisfy the shape. The old server-side `DataStoreProvider` is
now `ConfigurationGatewayDataStoreProvider` (in `Fdw.Services.Data`), which delegates store composition
to the connection-agnostic `ConfiguredDataStoreProvider` core — see
[05-03 DataNode Core Split](05-03-DataNode-Core-Split.md).

## What was removed

- **1-arg `RegisterRequiredServices(IServiceCollection)`** — deleted. Always 5 args.
- **2-arg `RegisterRequiredServices(IServiceCollection, ILoggerFactory?)`** — deleted.
- **`RegisterOverrides`** class — deleted. The TypeCollection's own attributes carry the
  default DataStore / Path / Container; no separate override registry.
- **`Parent*` named args on `[ServiceTypeCollection]`** — deleted. Parent metadata is derived
  from the attribute's `ServiceCategory` and the polymorphic dispatch contract.
- **`PolymorphicParentConfigurationProvider`** and **`EtlPipelineConfigurationStub`** —
  FDW-409 rejected this approach. The polymorphic pattern instead uses a real parent
  configuration record + per-variant typed-body records; see
  [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md).
- **`EtlPipelineConfigurationBase`** — gone; the concrete `EtlPipelineConfiguration` is the live type.
- **`InitializeUserConfiguration`** — deleted. Providers receive `Lazy<IConfigurationGateway>`
  via constructor and resolve it on first use; there is no propagation block.

## Related

- [ManagedConfiguration](03-01-ManagedConfiguration.md)
- [Configuration Guide](03-06-Configuration-Guide.md)
- [Service Domains Overview](06-01-Service-Domains-Overview.md)
- [Creating a Service Domain](06-02-Creating-Service-Domain.md)
- [JSON-Driven Configuration](03-05-JSON-Driven-Configuration.md)
- [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md)
