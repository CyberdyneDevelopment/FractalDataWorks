# Configuration Reload

Configuration cache invalidation is tag-based and decoupled from the per-domain provider. There is
no external reloader registry that propagates changes.

## Current Architecture

- `DefaultConfigurationProvider<TConfig, TCommand>` (and its per-domain subclasses such as
  `ConnectionConfigurationProvider`, `DataSetConfigurationProvider`,
  `DataStoreConfigurationProvider`) does not own a record cache and has no `EvictFromUserCache`
  method. Caching is built directly into `DataGatewayService` and `ConfigurationGateway`; cache
  state is held by the **singleton** `DataGatewayResultCache` (owns the IMemoryCache + tag sidecar).
- After a successful write, the write path calls
  `ICacheInvalidator.InvalidateByTag("{schema}.{table}")`. `DataGatewayResultCache` implements
  `ICacheInvalidator` and removes every cached entry tracked under that tag, so the next read
  repopulates from the database. The provider's own `Save()` / `Delete()` and
  `DynamicConfigurationWriter` both invalidate this way; eviction is decoupled from any provider
  scope.
- `IConfigurationReloader` / `DefaultConfigurationReloader` remain in
  `Fdw.Configuration.Writers` as a backward-compatibility shim — the implementation
  only writes a structured log entry and returns success. It is **not** the mechanism that
  propagates changes.
- For app configuration bound via `IOptions`, `IOptionsMonitor` fires automatically on the
  underlying `IConfigurationRoot` reload.

See `public/src/Fdw.Configuration.Writers/DefaultConfigurationReloader.cs`,
`public/src/Fdw.Services.Data/DataGatewayService.cs` (built-in caching),
`public/src/Fdw.Services.Data/ConfigurationGateway.cs` (built-in caching),
`public/src/Fdw.Services.Data/DataGatewayResultCache.cs` (singleton cache store), and the per-domain
`*ConfigurationProvider.cs` files for the authoritative behaviour.

## What Was Removed

The elaborate per-category reload pipeline previously documented here — `MsSqlConfigurationProvider`
per-category instances, `IMsSqlConfigurationProviderRegistry`,
`ICategoryConfigurationReloaderRegistry`, `MsSqlConfigurationProviderRegistry`,
`NullCategoryConfigurationReloaderRegistry`, `AddMsSqlConfiguration` extension method — no longer
exists. The gateway-caches-and-evicts-by-tag redesign replaced it.

## Related Documentation

- [ManagedConfiguration](03-01-ManagedConfiguration.md) — configuration type definitions
- [Configuration Writers](03-02-ConfigurationWriters.md) — write side and cache invalidation
