# Changelog

All notable changes to the FractalDataWorks framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-rc.1] - 2026-07-02 — Initial release

This is the first public release of FractalDataWorks (FDW): a .NET framework for building
data-centric services (APIs, ETL pipelines, schedulers, and UIs) on a small set of consistent
mechanisms — a fail-loud result type, extensible-enum TypeCollections, a single data-access
gateway, database-backed configuration, and a render-agnostic UI model. The sections below
describe the framework **as shipped**, not as a diff from an internal pre-release.

### Added

**Results & messaging**
- `IGenericResult` / `IGenericResult<T>` — the universal return type for anything that can fail.
  Methods never return `null` and never throw for expected failure; they return a result carrying
  a `ResultCode`, a message chain, and root-cause tracking (`IsSuccess`/`Value`/`Status`/`CodeChain`/`RootCause`/`IsEmpty`)
- `IGenericMessage` / `GenericMessage` — structured message type (`Message`/`Code`/`Source`/severity),
  produced by the MessageLogging generator rather than hand-built
- Categorized `ResultCode` catalog — every code's number doubles as its `Id` and `EventId`; the
  leading digit selects one of 11 handling categories via the closed `ResultCategories`
  TypeCollection, including `ForbiddenCategory` (403) and `GatewayTimeoutCategory` (504) alongside
  Success/Validation/Auth/Conflict/Dependency/Transient/Configuration/Missing/Internal
- `[MessageLogging]` source generator — `[MessageLoggingTypeCode("PREFIX")]` partial classes with
  `[MessageLogging]` partial methods that both log and return an `IGenericMessage`; raw `ILogger`
  calls are disallowed by analyzer (FDW003)
- `logger ?? NullLogger<T>.Instance` as the one sanctioned fallback pattern in the codebase

**TypeCollections, source generators & analyzers**
- TypeCollections — the framework's extensible-enum mechanism: `[TypeCollection]` families of
  `[TypeOption]` members looked up by `ById`/`ByName`/`All`, auto-registered at module load.
  Failed lookups return a `NotFound` sentinel, never `null`
- ServiceTypeCollections — `[ServiceTypeCollection]` plugin families driving a three-phase DI
  lifecycle (`Configure → Register → Initialize` on the collection; `RegisterRequiredServices` /
  `RegisterFactory` per option). This is the only service-registration mechanism in FDW — no
  ad-hoc `AddXxx` extensions
- Source generators covering configuration DDL + validators, POCO row↔object mappers
  (`[GenerateMapper]`), MessageLogging, TypeCollection/ServiceTypeCollection scaffolding, and
  cross-assembly registration
- FDW001–FDW023 Roslyn analyzer family enforcing the no-fallback/no-raw-logging/TypeCollection/
  ServiceType conventions at build time

**Data access (DataGateway)**
- `IDataGateway` — the only data-access path in the framework. Addressing is supplied per call via
  `DataStoreTarget(DataStore, Path?, Container)` or `DataSetTarget(DataSet)`; commands never carry
  raw connection strings or schema/table strings
- Command set: Query / Insert / BulkInsert / Update / Delete / Find / Truncate, plus DDL commands
  (CreateTable/AlterTable/Index/Schema/View and drops), all reached through `IDataGateway.Execute`
- Gateway-native transactions (`BeginTransaction`) — a scope's calls run on one physical connection
  in one native transaction, with fail-loud behavior when a backend doesn't support transactions
- Streaming cursor (`OpenRecordSource`) for backends that support it, with fallback to materialized
  execution when they don't
- Caching built into the gateway itself — a tag-invalidated result cache gated by configuration,
  with a per-call `useCache:false` escape hatch; no cache decorators or per-provider caches
- Per-connection limit enforcement (rate, concurrency, result size, and query/byte budgets)
- `IConfigurationGateway` — the same gateway model specialized for reading/writing ConfigurationDb

**Connections, SQL dialects & data motion**
- Six connection backends: MsSql, PostgreSql, Sqlite, Http, FileSystem, and RoslynWorkspace, each
  exposing capability marker interfaces so callers never branch on connection type
- A shared SQL translator (`SqlDataCommandTranslatorBase`) plus a per-backend `ISqlDialect`
  (TSql / PlPgSql / Sqlite) — adding a new SQL backend means adding a dialect and a driver, not a
  new translator
- DataSets — a `DataSetTypes` strategy dispatch (Simple / Compound / Federated) so a named DataSet
  resolves to the right execution strategy at runtime
- Pipeline and transform infrastructure: ETL pipeline steps, field-level data transformers, and
  DI-managed transformation services, plus pipeline/task type collections for Extract/Transform/
  Load/Validate/Branch/Notify style orchestration
- Data lineage node/edge/graph model with execution integration

**Configuration**
- `[ManagedConfiguration]` — a class-level attribute that drives DDL generation, validation, and
  UI form metadata for configuration POCOs
- Parent-header + typed-body pattern for polymorphic configuration (e.g. a Connection header with
  one typed body per connection kind), so runtime fields land on the correct table
- One configuration-provider mechanism (`DefaultConfigurationProvider<TConfig,TCommand>`) composing
  reads (`Get(name)/Get(id)/Get()`) and cascading writes (`Save`) with version-on-write and
  tag-based cache invalidation
- App-shipped `configurationSchema.json` — each entry-point app declares the connections,
  secret managers, and data stores it needs to reach its configuration store

**Service domains**
- Connections and SecretManagers (MsSql, Sqlite, Azure Key Vault, environment variable, user secrets)
- Authentication via OpenIddict (OAuth2/OIDC authorization server, RS256 tokens, permission claims
  baked at issuance, client-credentials support)
- Authorization — org-scoped role/permission model enforced through a permission requirement/handler
  pair, with an effective-permission resolver
- Multitenancy — two-level Tenant→Org isolation with tenant-aware connection routing and cache keys
- Health monitoring — a `HealthMonitorTypes` collection with a local health monitor implementation
  and provider
- Scheduling (Cron/Interval/Once/Manual triggers) driving ETL dispatch from a background loop
- Execution/Operations tracking with a validated state machine and escalation policies
- Users/Credentials, DataVault, Quality, Workflows, Notifications (Console/Email/Webhook/System),
  Messaging, Session state, Settings, Audit, Agents, and Resiliency (Polly-backed retry/failover)

**Hosting & UI**
- Hosting middleware: security headers, an internal API key gate for machine-to-machine endpoints,
  forwarded-headers support, fail-loud bootstrap validation, and bounded Serilog flush on shutdown
- Render-agnostic UI — `UI.Abstractions` contracts and an `IUIRenderer` seam, with a Spectre console
  renderer as the reference implementation
- Headless Blazor components — a Context/Provider/ProviderLog triple that keeps logic in
  render-agnostic providers so skins stay presentation-only
- Realtime hubs — a `RealTimeHubBase<TClient>` base plus a `RealTimeHubs` TypeCollection so each
  SignalR hub is registered as a single type option instead of app-specific wiring
- `Fdw.DevSession.Abstractions` — worktree/branch isolation contracts (`IWorktreeEngine`,
  `IIsolationLevel`) for tooling that needs to stage isolated copies of a working tree

[1.0.0-rc.1]: https://github.com/CyberdyneDevelopment/FractalDataWorks/releases/tag/v1.0.0-rc.1
