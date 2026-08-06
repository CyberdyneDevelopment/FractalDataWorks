# Configuration Provider Caching

## The Gateway Caches, Not the Provider

`DefaultConfigurationProvider<TConfig, TCommand>` is the loader for runtime configuration, but it
does **not** hold a cache of records. Every `Get(name)`, `Get(id)`, and `Get()` builds a command
and calls `IConfigurationGateway.Execute<…>(cmd)` — there is no `ConcurrentDictionary` of records
inside the provider. (The one `ConcurrentDictionary` it does hold, `_configPropCache`, is a
reflection `PropertyInfo` cache used by cascade-save, not a data cache.)

Caching is built directly into `DataGatewayService` and `ConfigurationGateway` — there is **no**
separate `CachingDataGateway` decorator. The gateway checks `DataGatewayOptions.EnableCache` and
(for `DataGatewayService`) `CachePolicy.IsEnabled(command)` before reading or writing the cache.

`IDataGateway` is registered as a scoped decorator chain (LimitEnforcementDataGateway →
DataGatewayService); a new chain is created per request scope. Cross-request cache STATE lives in
the **singleton** `DataGatewayResultCache` (it owns the IMemoryCache + tag sidecar and implements
`ICacheInvalidator`); `DataGatewayService` holds a direct reference to it. Cache entries are shared
across requests (correct) while the gateway instances themselves are not (safe). Cache keys are
tenant/org-discriminated so entries from one tenant do not leak to another.

The gateway caches command results keyed by the computed cache key plus result type, and tags each
entry with `"{schema}.{table}"` so they can be evicted together. Cache invalidation runs through
`ICacheInvalidator.InvalidateByTag(...)`.

There are two cache-control knobs on `IDataGateway.Execute<T>`:

| Knob | Behaviour |
|------|-----------|
| `DataGatewayOptions.EnableCache = true` (default) | Gateway reads and writes the cache. ETL/Scheduler services set this to `false` for a fully cacheless path. |
| `useCache: false` (per-call override) | Skips the cache read but still writes on success. Use this for force-refresh. |

| Layer | Role |
|-------|------|
| `*ConfigurationProvider` (`DefaultConfigurationProvider<TConfig, TCommand>`) | Builds commands and calls the gateway on every read. No per-record cache. |
| `IConfigurationGateway` (`ConfigurationGateway`) | The runtime config read source — reads ConfigurationDb tables. Caching built-in; all reads are cached when `EnableCache=true`. |
| `DataGatewayService` | Routes data commands to connections. Caching built-in; respects `CachePolicy.IsEnabled(command)` per-command opt-in and the `EnableCache` option. |
| `DataGatewayResultCache` | **Singleton** — owns IMemoryCache + tag sidecar; implements `ICacheInvalidator`. Never instantiated per-request. |

Inject the domain provider. Call `Get(name)`, `Get(id)`, or `Get()` (all items) — each returns an
`IGenericResult<…>`. There is one record per name; no merge and no precedence.

## The IOptionsMonitor merge

`DefaultConfigurationProvider` carries a `UseOptionsMonitor` flag (default `true`). When it is on,
reads first look at an `IOptionsMonitor<List<TConfig>>` snapshot and merge it with the gateway
result. For runtime providers (Connection, SecretManager, Authentication, etc.) the merge is
**disabled** (`UseOptionsMonitor = false`) at registration, so reads go straight to the gateway and
the ConfigurationDb row is the only source.

## Cache Invalidation

There is no `EvictFromUserCache` / `EvictAllFromUserCache` on the provider. After a successful
write, the provider's `Save()` / `Delete()` calls `ICacheInvalidator.InvalidateByTag(tag)` where
`tag` is `Commands().CacheTag(PathName)` — i.e. `"{schema}.{table}"`. The singleton
`DataGatewayResultCache` implements `ICacheInvalidator`: invalidating a tag removes every cached
entry tracked under it, so the next read repopulates from the database. Invalidation failure is
logged as a warning and never fails the write.

## Related Documentation

- [ManagedConfiguration](03-01-ManagedConfiguration.md) — `[ManagedConfiguration]` attribute
- [Configuration Writers](03-02-ConfigurationWriters.md) — write paths and version-on-write
- [DataGateway Pattern](05-01-DataGateway-Pattern.md) — How providers query ConfigurationDb
