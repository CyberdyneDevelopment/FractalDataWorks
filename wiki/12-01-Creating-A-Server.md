# 12-01 Creating a Server

This page covers the general shape of an FDW server-side application. For server-type-specific tutorials, see:

- [14-01 Building an API Server](14-01-Building-An-API-Server.md)
- [14-02 Building an ETL Server](14-02-Building-An-ETL-Server.md)
- [14-03 Building a Scheduler Server](14-03-Building-A-Scheduler-Server.md)

The canonical reference Program.cs files live in the reference repositories:

- `reference-api/public/src/Reference.Api/Program.cs`
- `reference-etl/public/src/Reference.Etl.Server/Program.cs`
- `reference-scheduler/public/src/Reference.Scheduler.Server/Program.cs`

## General Startup Shape

Every FDW server follows the same overall flow. The per-domain `Configure`/`Register`/`Initialize`
ceremony is gone — one **PlatformServices sweep** drives every domain.

1. **Load `configurationSchema.json`** via `builder.Services.AddConfigurationGateway<TConnectionFactory, TSecretManager>("configurationSchema.json")` at the very start of `Program.cs`. This declares the ConfigurationDb connection — every other connection (OpsDb, NflDb, user-defined) is runtime data inside ConfigurationDb (AuthDb is also declared here because the login flow needs it before the gateway is live).
2. **Configure framework logging** via `builder.AddFrameworkSerilog("AppName")`. The returned `ILoggerFactory` is passed to the PlatformServices sweep.
3. **Configure OpenTelemetry** via `builder.Services.AddFrameworkOpenTelemetry(builder.Configuration, builder.Environment, "AppName")`.
4. **Register the `Lazy<IDataGateway>` singleton** before any provider — OpenIddict stores, connection factories, and config providers take `Lazy<IDataGateway>` so they defer gateway resolution to first use, avoiding circular startup dependencies.
5. **Run PlatformServices sweep (Phase 1 — Configure + Register, before Build):**
   ```csharp
   PlatformServices.Configure(builder, loggerFactory);
   PlatformServices.Register(builder.Services, loggerFactory);
   ```
   Every `[ServiceTypeCollection]` discovered by the generated module initializer participates — DataGateway, SecretManager, Connection, DataStore, DataSet, Authentication, ETL, Scheduler, Notifications, Transformations, Workflows, RealTimeHubs, and so on. There are **no** hand-written per-domain `XxxTypes.Configure(...)` / `DataStoreProvider.Configure(...)` blocks. Multitenancy is a "declared choice" domain (`MultitenancyTypes`): its self-selecting `Configure` resolves the single option named by `ConfigurationSchema.Multitenancy` (from `configurationSchema.json` — `Sql` for the API, `SingleTenant` for gateway-less/simple hosts) and drives that one option's `Configure`/`RegisterRequiredServices`, so it participates in the same discovery without a separate block.
6. **Register standalone hosting helpers** that aren't `[ServiceTypeCollection]` domains (`AddFrameworkOperations`, `AddFrameworkHealthMonitoring`, `AddFrameworkConfigurationWriters`, `AddFrameworkPipelineBackgroundExecutor`, `AddFrameworkCors`, `AddFrameworkRateLimiting`, FastEndpoints/Swagger).
7. **`var app = builder.Build();`**
8. **Run PlatformServices sweep (Phase 2 — Initialize, after Build):**
   ```csharp
   PlatformServices.Initialize(app.Services, loggerFactory);
   ```
   One call runs every discovered domain's `Initialize` in dependency-safe **Group** order (SecretManager → Connection → DataGateway → DataVault → CredentialService → … → DataStore → DataSet → rest). The Group DAG encodes the order, so no hand-driven prerequisite pre-calls are needed. A domain that doesn't expose the generated three-phase shape (e.g. `OrchestrationTypes`) is not discovered and is still driven by hand.
9. **Map real-time hubs** with `app.MapRealTimeHubs(loggerFactory)` — the `RealTimeHubs` *registration* is part of discovery, but endpoint **mapping** is a post-Build call each host still makes.
10. **Configure middleware** (forwarded headers, CORS, rate limiting, authentication/authorization, FastEndpoints, etc.).
11. **`await app.RunAsync();`**

> **Discovery is opt-in per app.** `PlatformServices` is populated by the `[ModuleInitializer]` emitted
> by `Fdw.Services.Registration.SourceGenerators` when the entry-point assembly loads — an app gets this
> behavior only if it references that generator (plus `Fdw.Services.Registration`). Hand-written
> three-phase provider classes that aren't themselves TypeCollections (e.g. `DataSetProvider`,
> `ConfigurationGatewayDataStoreProvider`, `RealTimeHubs`) join discovery by carrying the
> `[PlatformServiceProvider]` attribute.

## Required Package References

Every FDW server references at minimum:

- `Fdw.MessageLogging.Abstractions`
- `Fdw.MessageLogging.SourceGenerators` (analyzer-only)
- `Fdw.Registration.SourceGenerators` (analyzer-only — emits `[ModuleInitializer]` to register `[ServiceTypeOption]` instances into their TypeCollections at assembly load)
- `Fdw.Services.Registration` + `Fdw.Services.Registration.SourceGenerators` (analyzer-only — emits the `[ModuleInitializer]` that populates `PlatformServices` from every discovered `[ServiceTypeCollection]` / `[PlatformServiceProvider]`)
- `Fdw.Hosting`, `Fdw.Hosting.MsSql`
- Per-domain implementation packages for whichever ServiceTypeCollections the server hosts
- Per-domain endpoint packages (for API/ETL/Scheduler servers using FastEndpoints)

## See Also

- [03-05 Configuration Startup JSON](03-05-JSON-Driven-Configuration.md) — `configurationSchema.json` shape
- [20-02 Service Startup Order](20-02-Service-Startup-Order.md) — three-phase registration
- [12-10 Secret Management](12-10-Secret-Management.md) — `FDW_SECRET_*` and secret-manager wiring
- [12-13 OpsDb Configuration](12-13-OpsDb-Configuration.md) — operations runtime data
