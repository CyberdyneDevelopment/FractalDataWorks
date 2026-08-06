# Configuration Writers

Configuration writers persist `[ManagedConfiguration]` records through `IDataGateway` and invalidate
the gateway cache by tag so subsequent reads return the new state.

## Current Writer Interfaces

| Interface | Location | Purpose |
|-----------|----------|---------|
| `IDynamicConfigurationWriter` | `Fdw.Configuration.Writers.Abstractions` | Type-name-dispatch writer used by the generic admin UI for arbitrary configuration types at runtime. Saves and deletes by `(typeName, dictionary)` rather than `<T>`. |
| `IServiceConfigurationWriter<TConfig>` | `Fdw.Services.Abstractions` | Per-domain typed write view (`Save` / `Delete`) exposed by the domain provider that owns the write path (e.g. connections, datasets). |
| `IConfigurationReloader` | `Fdw.Configuration.Writers.Abstractions` | Backward-compatibility shim. The default implementation only logs — actual cache invalidation runs through `ICacheInvalidator.InvalidateByTag(...)`. |

There is no single `IConfigurationWriter<T>` interface with multiple backend implementations.
`DynamicConfigurationWriter` (in `Fdw.Configuration.Writers`) writes via `IDataGateway`
against the data store and container resolved through `IConfigurationContainerLookup`, then
invalidates the `"{schema}.{table}"` cache tag through `ICacheInvalidator`.

## Registration

Configuration writers register through the standard hosting extension:

```csharp
builder.Services.AddFrameworkConfigurationWriters(builder.Configuration, loggerFactory);
```

`AddFrameworkConfigurationWriters` (in `Fdw.Hosting/Extensions/ServiceTypeExtensions.cs`):

1. Calls `ConfigurationWriterRegistration.Register`, which:
   - Binds `ConfigurationWriterOptions` from the `ConfigurationWriter` section
   - Registers the `IConfigurationRoot` (so the reloader can trigger `Reload()`)
   - Registers `IConfigurationReloader → DefaultConfigurationReloader`
   - Registers `IDynamicConfigurationWriter → DynamicConfigurationWriter`, whose constructor
     resolves `IDataGateway`, `IConfigurationReloader`, `IConfigurationContainerLookup`,
     `IOptions<ConfigurationWriterOptions>`, an optional `ILogger<DynamicConfigurationWriter>`, and a
     `Lazy<ICacheInvalidator?>`
2. Then registers `IConfigurationMaintenanceService → ConfigurationMaintenanceService` directly
   (this registration lives in `AddFrameworkConfigurationWriters`, not inside
   `ConfigurationWriterRegistration.Register`)

After `builder.Build()`, call `ConfigurationWriterRegistration.Initialize(app.Services, loggerFactory)`
to eagerly resolve `IDynamicConfigurationWriter`.

## Per-Domain Typed Save Paths

Top-level named configurations (Connection, DataStore, DataSet, Pipeline, Schedule, Settings, Role,
SecretManager) are written through their domain provider's `Save()` / `Delete()` (the provider
implements `IServiceConfigurationWriter<TConfig>`), which builds and executes the appropriate command
via the gateway and invalidates the `"{schema}.{table}"` cache tag. Child configuration records
(e.g. `FieldMappingTransform` children) use `ConfigurationSaveCommand<T>` via `IDataGateway` directly.

## See Also

- [03-01 ManagedConfiguration](03-01-ManagedConfiguration.md) — configuration type definitions
- [03-03 Configuration Reload](03-03-Per-Category-Configuration-Reload.md) — current cache invalidation model
- [03-05 Configuration Provider Registration Pattern](03-05-Configuration-Provider-Registration-Pattern.md) — three-phase provider registration
