# Configuration Guide

There are **three** kinds of configuration, and each has exactly one loader:

| Kind | Loaded from | Loader | Examples |
|---|---|---|---|
| **Shipped schema** | `configurationSchema.json` (shipped in the entry-point app's content root) | `IConfigurationGateway` via `AddConfigurationGateway<...>` | The `ConfigurationDb` connection (and any other connection the app must hold to reach configuration storage), its `SecretManager`, its `DataStore` shape. |
| **Runtime** | ConfigurationDb tables, per-domain schema (`conn`, `data`, `auth`, `pipe`, …) | A domain-specific `*ConfigurationProvider` reading through `IConfigurationGateway` | All other connections, datastores, datasets, pipelines, schedules, themes. |
| **App** | `appsettings.json` (or environment / Azure App Configuration) bound to `IOptions<T>` | `IConfiguration` / `IOptionsMonitor<T>` | Serilog, OpenTelemetry, JWT keys, support contact email, rate-limit policies. |

There is **one** source per item — no merge, no precedence. If you can't decide which kind
something is, ask: *can a user create more of these at runtime?* If yes, it's runtime. If it
is a connection the app must already hold to *reach* configuration storage, it's declared in
the shipped schema. Otherwise it's app config.

## The shipped configuration schema

`configurationSchema.json` ships in the content root of each entry-point app. The host loads
it via `AddConfigurationGateway<TConnectionFactory, TSecretManager>(filename)`, which
deserialises it with System.Text.Json — a custom `JsonConverter` dispatches the polymorphic
`ConnectionConfiguration` / `SecretManagerConfiguration` bodies on the `ServiceOptionType`
discriminator — and feeds it to `IConfigurationGateway`.

The JSON has three top-level lists under `ConfigurationSchema`: `Connections`,
`SecretManagers`, and `DataStores`. Each connection's and secret manager's body nests under a
`Configuration` object. The connections declared here are ordinary `Connection`
configurations — the same type as a connection stored as a runtime row — they are simply
**declared in the file the app ships with** rather than read from ConfigurationDb (which the
app needs a connection to reach in the first place). See
[JSON-Driven Configuration](03-05-JSON-Driven-Configuration.md) for the full shape and a
worked example.

## Runtime configuration

Every domain that owns user-writable configuration has the same machinery:

1. A **`[ManagedConfiguration]` POCO** — partial class, source-generated mapper, tagged with
   `ServiceCategory` / `ServiceType` so the DDL generator and loader know where it maps. See
   [ManagedConfiguration](03-01-ManagedConfiguration.md).
2. A **`*ConfigurationProvider`** — exposes `Get(name)`, `Get(id)`, and `Get()` (all items),
   each returning `IGenericResult<…>`. Reads through `IConfigurationGateway` against the
   ConfigurationDb tables; results are cached by the built-in caching inside `ConfigurationGateway`
   (backed by the singleton `DataGatewayResultCache`) and invalidated on writes.
3. **Writes** — top-level named configs persist via the **domain provider's `Save()` /
   `Delete()`** (e.g. `_dataStoreProvider.Save(config)`), which applies version-on-write and
   tag-based cache invalidation. Generic admin types use `IDynamicConfigurationWriter`; child
   records use `ConfigurationSaveCommand<T>` via the DataGateway. See
   [Configuration Writers](03-02-ConfigurationWriters.md).
4. A **`<Domain>ConfigurationProvider.RegisterDomainServices(services, …)`** call in
   `Program.cs` wires the provider against ConfigurationDb's path for that domain.

Cache invalidation is covered in
[Per-Category Configuration Reload](03-03-Per-Category-Configuration-Reload.md): a writer calls
`ICacheInvalidator.InvalidateByTag("{schema}.{table}")` and the next read repopulates lazily.

## App configuration

Standard ASP.NET Core: `appsettings.json`, environment variables, command-line args, all
projected onto `IConfiguration`. Bind to `IOptions<T>` / `IOptionsMonitor<T>` / `IOptionsSnapshot<T>`
the same way you would in any other ASP.NET Core app. This includes:

- Serilog sinks and enrichers.
- OpenTelemetry exporter endpoints.
- JWT/OpenIddict signing keys, issuer, audience, lifetime.
- Service endpoints used by `PipelineJobHttpClient` / `ScheduleHttpClient`.

## Decision tree

```
Is this a Connection / SecretManager / DataStore the host needs to *reach* ConfigurationDb?
  YES → declare it in configurationSchema.json (the shipped schema)
  NO ↓
Is this a domain entity users create/edit at runtime via the admin UI?
  YES → ConfigurationDb table + the domain's *ConfigurationProvider (runtime)
  NO ↓
Is this app behaviour (logging, telemetry, signing keys, service URLs)?
  YES → appsettings.json + IOptions<T> (app)
```

## Why one source per item

The previous two-source merge had three failure modes that motivated removal:

1. **Ambiguity on collision** — when system and user both defined an entry with the same
   name, the precedence rule ("system always wins") silently shadowed user intent.
2. **Pre-startup SQL** — the system loader needed a raw `SqlConnection` before DI was built,
   creating singleton loops every time the configuration pipeline grew.
3. **Two code paths for one concept** — every domain shipped a merger, a writer-side guard,
   and a UI-side filter to hide system rows from delete. One source means zero of those.

JSON-declared connections plus runtime-only domain providers eliminate the merge and the
pre-startup loader at the same time.

## Related

- [JSON-Driven Configuration](03-05-JSON-Driven-Configuration.md)
- [ManagedConfiguration](03-01-ManagedConfiguration.md)
- [Configuration Writers](03-02-ConfigurationWriters.md)
- [Per-Category Configuration Reload](03-03-Per-Category-Configuration-Reload.md)
- [Cache-Backed Providers](03-04-Cache-Backed-Providers.md)
- [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md)
