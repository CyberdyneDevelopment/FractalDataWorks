# FDW Capabilities Catalog

**What FractalDataWorks actually brings to the table** — a single, source-verified inventory of the
features, components, and patterns present in the live `Fdw.*` tree, as of **2026-07-02**, the
**1.0.1-rc.1 first public release**.

This is the answer to "what does FDW provide?" for someone meeting the framework cold. Each entry
cites a canonical source anchor; counts are from `fractaldataworks/public/src` excluding generated
output. "~" marks an approximate count.

> How to keep this honest: every claim here is grepped/read against source. When a subsystem changes
> materially, re-verify the affected section and bump the date. Do not copy numbers forward blindly.

---

## 1. Core result & messaging primitives

| Capability | What it is | Anchor |
|---|---|---|
| **`IGenericResult` / `IGenericResult<T>`** | The universal return type. Success/failure monad — methods that can fail **never return null and never throw**; they return a result carrying a `ResultCode` + message chain. `IsSuccess`/`Value`/`Status`/`CodeChain`/`RootCause`/`IsEmpty`. | `Fdw.Results/GenericResult.cs`, `Fdw.Abstractions/IGenericResult.cs` |
| **`IGenericMessage`** | Structured message (`Message`/`Code`/`Source`; severity on `GenericMessage`). Produced by the MessageLogging generator, not hand-built. | `Fdw.Abstractions/IGenericMessage.cs`, `Fdw.Messages/GenericMessage.cs` |
| **Categorized ResultCode catalog** | ~560 `*Code` options across ~52 `*ResultCodes` collections. One number == `Id` == `EventId` == numeric part of `Code` (`"{PREFIX}-{number}"`), `Domain == "{PREFIX}"`; `number / 10000` selects a handling category via the closed **11-option** `ResultCategories` TypeCollection. | `Fdw.Results/ResultCategories.cs`, `Fdw.Results.Abstractions/ResultCodeBase.cs:60`, canonical option `Fdw.Operations.Abstractions/Results/ExecutionItemNotFoundCode.cs` |
| **`ResultCategories` (11 categories)** | Closed TypeCollection carrying `IsFailure`/`IsRetryable`/`HttpStatus`/`ClientMessage`/`ClientAction` per category — the authoritative HTTP + client-safe-copy source for every code in that band. Bands 1–9 (5-digit codes): `Success`(200)/`Validation`(400)/`Missing`(404)/`Conflict`(409)/`Auth`(401)/`Configuration`(500)/`Dependency`(502)/`Transient`(503)/`Internal`(500). Bands 10–11 (6-digit, 100000+): **`Forbidden`(403)** — authenticated but not permitted, distinct from `Auth`'s 401 — and **`GatewayTimeout`(504)** — a downstream dependency didn't respond in time, distinct from `Transient`'s 503. FDW owns all 11 options; consumers only add specific codes in the open band. | `Fdw.Results/ForbiddenCategory.cs`, `Fdw.Results/GatewayTimeoutCategory.cs`, `Fdw.Results.Abstractions/ResultCategoryBase.cs` |
| **Category-based HTTP mapper** | `ResultHttpStatusMapper.Map(result, httpContext)` extracts the result's `ResultCode`, derives its category via `ResultCategories.ById(code.Id / 10000)`, and returns `(category.HttpStatus, ErrorResponse)` built from `ClientMessage`/`ClientAction`/`IsRetryable` — never a per-code string table, so status/retryability/client copy can't drift out of sync when codes are renumbered. Falls back to 500/generic only for an uncategorized/legacy code or no code at all. | `Fdw.Web.RestEndpoints/ErrorMapping/ResultHttpStatusMapper.cs` |
| **MessageLogging** | `[MessageLoggingTypeCode("PREFIX")] static partial class` + `[MessageLogging] partial` methods returning `IGenericMessage`; the generator emits a body that **logs AND returns** the structured message. Raw `ILogger.Log*` is forbidden (FDW003). 300+ log methods. | `Fdw.MessageLogging.Abstractions/MessageLoggingAttribute.cs`, canonical `Fdw.Services.Connections/Logging/ConnectionConfigurationProviderLog.cs` |
| **NullLogger fallback** | The single sanctioned `??` in the codebase: `logger ?? NullLogger<T>.Instance`. | convention (every ctor taking `ILogger<T>`) |

## 2. TypeCollections, source generators & analyzers

| Capability | What it is | Anchor |
|---|---|---|
| **TypeCollections** | Extensible-enum mechanism: 301 `[TypeCollection]` families holding **~2260** `[TypeOption]` members, looked up by `ById`/`ByName`/`All`, populated at module init (package reference IS the registration). 6 variants (mutable/immutable × service-registration/plain). Failed lookups return a `NotFound` sentinel, never null. | `Fdw.Collections/Attributes/*`, canonical `Fdw.Services.Connections/ConnectionTypes.cs` |
| **ServiceTypeCollections** | 27 `[ServiceTypeCollection]` plugin families driving the 3-phase DI lifecycle: collection `Configure → Register → Initialize`; per-option `RegisterRequiredServices / RegisterFactory`. 11 generate a domain provider (`GenerateProvider = true`, e.g. Connections, HealthMonitor). The ONLY registration mechanism — no `AddXxx` extensions in app code. Every collection and option phase method is swappable at runtime (`Configuration`/`Registration`/`Initialization`/`RegistrationFactory`), defaulting to the original behavior. | `Fdw.Services.Abstractions/IServiceType.cs`, canonical `Fdw.Services.Connections.Http/HttpConnectionType.cs`, `Fdw.Services.HealthChecks.Monitoring/HealthMonitorTypes.cs` |
| **Source generators** | 9 `*.SourceGenerators` projects / ~17 `[Generator]` classes: ConfigurationSourceGenerator (DDL + validator + ConfigurationTypes), PocoMapperGenerator (`[GenerateMapper]`, ~205 production usages), MessageLogging/LoggerMessage generator, the TypeCollection/ServiceTypeCollection generator family (7 generators in `Fdw.Collections.SourceGenerators`), the 4 Registration module-initializer generators, RazorConsole generator, ConfigurationUI generator. | `Fdw.*.SourceGenerators/`, `Fdw.Data.SourceGenerators/PocoMapperGenerator.cs`, `Fdw.Configuration.SourceGenerators/ConfigurationSourceGenerator.cs` |
| **Analyzers** | 40 `DiagnosticAnalyzer` classes — the **FDW001–FDW023** rule family (incl. FDW022 SwallowedException, FDW023 BroadCatch) plus the TC/ENH/COLL/SVCTYPE authoring-analyzer families that enforce TypeCollection/ServiceType/collection conventions at build. | `Fdw.Analyzers/`, `Fdw.Collections.Analyzers/`, `Fdw.ServiceTypes.Analyzers/` |

## 3. Data access (DataGateway)

| Capability | What it is | Anchor |
|---|---|---|
| **Target-typed `IDataGateway`** | The ONLY data-access path. Addressing lives OFF the command — supplied per call via `DataStoreTarget(DataStore, Path?, Container)` or `DataSetTarget(DataSet)`; the connection resolves from `DataStore.ConnectionId`. Fluent builders terminate in `Build()` → `DataGatewayCall`. No raw ADO.NET / connection strings / schema-table strings above the connection layer. | `Fdw.Services.Data.Abstractions/IDataGateway.cs`, `Targeting/{DataStoreTarget,DataSetTarget,DataGatewayCall}.cs` |
| **Command set** | A `DataCommands` TypeCollection: Query / Insert / BulkInsert / Update / Delete / Find (`FindResult<T>`) / Truncate, plus config-write `ConfigurationSaveCommand`/`ConfigurationDeleteCommand` (child-config only). Fluent authoring API (`DataQuery.From/Where/OrderBy/Limit/...`). | `Fdw.Commands.Data/Commands/`, `Fdw.Commands.Data.Abstractions/Commands/DataCommands.cs` |
| **DDL command family** | `DdlCommandTypes` (10 options): CreateTable/AlterTable/Index/Schema/View + drops, with `AlterTableOperationTypes` and `ForeignKeyActions`. DDL also flows through `IDataGateway.Execute` with a target — no raw schema strings. | `Fdw.Commands.Data/Ddl/`, `DdlCommandTypes.cs` |
| **Gateway transactions** | `BeginTransaction(connectionName)` → `IGenericResult<IDataGatewayTransaction>`; all calls on the scope run on one physical connection in one native transaction (Commit/Rollback, implicit rollback on dispose). **Fails loud** when the backend isn't `ITransactionalDataConnection` (e.g. REST). | `IDataGateway.cs:99`, `Fdw.Services.Data/DataGatewayTransaction.cs` |
| **Streaming cursor** | `OpenRecordSource(command, target)` → disposable `IRecordSource<DataRecord>` exposing rows as `ReadOnlySpan<object?>` over a shared schema flyweight. Optional connection capability (feature-detected via `IRecordSourceConnection`); fails loud / falls back to materialized Execute. | `IDataGateway.cs:78`, `IRecordSourceConnection.cs` |
| **Inlined caching** | Caching is built INTO `DataGatewayService`: a singleton `DataGatewayResultCache` (IMemoryCache + tag→keys sidecar) gated by `DataGatewayOptions.EnableCache` + `CachePolicy`; per-call `useCache:false` = force-refresh; tenant/org-discriminated keys; tag invalidation via `ICacheInvalidator` on writes. api=cached / etl,scheduler=cacheless by construction. (No decorator chain.) | `Fdw.Services.Data/DataGatewayService.cs`, `DataGatewayResultCache.cs` |
| **Per-connection limit enforcement** | `LimitEnforcementDataGateway` wraps the gateway to enforce rate / concurrency / max-result-size / daily query+byte budgets / timeouts per connection. | `Fdw.Services.Data/Limits/LimitEnforcementDataGateway.cs` |
| **`IConfigurationGateway`** | `IDataGateway` specialization for ConfigurationDb: a DataStores tree built from `configurationSchema.json` (no DB round-trip for schema metadata) + reflection-free `Execute(command, target[, Type])` overloads for the runtime-typed save/compose cascade. | `Fdw.Services.Data.Abstractions/IConfigurationGateway.cs`, `Fdw.Services.Data/ConfigurationGateway.cs` |
| **IDataNode tree** | Uniform node model — `IDataNode` (`Nodes`/`Node(name)→IGenericResult`), `IDataStore` (carries `ConnectionId` + `Paths`), `IDataContainer`/`IStorageContainer` (Keys/ReferencingKeys). Containers resolve on demand via `IDataStoreProvider.GetContainer`. | `Fdw.Data.Abstractions/DataNodes/*` |

## 4. Connections, dialects & data motion

| Capability | What it is | Anchor |
|---|---|---|
| **6 connection backends** | `ConnectionTypeBase` subclasses tagged `[ServiceTypeOption(typeof(ConnectionTypes))]`: **MsSql, PostgreSql, Sqlite, Http, FileSystem, RoslynWorkspace** — each with factory + capability marker interfaces (`ISupportsContainerTypes`, `ISupportsWriteModes`, `ISupportsCalculationPushdown`, …) and a `CommandCapabilityTypes` supported-command set. Connection type is invisible above the connection layer. | `Fdw.Services.Connections.{MsSql,PostgreSql,Sqlite,Http,FileSystem,RoslynWorkspace}/`, `ConnectionTypes.cs` |
| **SQL dialect seam** | All SQL backends share one `SqlDataCommandTranslatorBase` + an `ISqlDialect` (Quote, ParameterPrefix, paging, schema-namespace support) that rides on the path. 3 dialects: TSql / PlPgSql / Sqlite. Adding a SQL backend = dialect + driver. Fail-loud on missing dialect. | `Fdw.Services.Connections.Abstractions/ISqlDialect.cs`, `Fdw.Services.Connections.Sql/SqlDataCommandTranslatorBase.cs` |
| **DataSets (dispatch by strategy KIND)** | `DataSetTypes` = 3 strategy kinds — **Simple / Compound / Federated** — each owning `Execute<T>(IDataSetExecutionContext, IDataCommand, ct)`. A DataSet name is an instance resolved live from `DataSetConfigurationProvider.Get`; gateway dispatches on `ServiceOptionType`. | `Fdw.Data.DataSets.Abstractions/IDataSetType.cs`, `Fdw.Services.Data/DataSets/{Simple,Compound,Federated}DataSetType.cs` |
| **Transforms (3 dispatch mechanisms)** | (1) ETL pipeline transform STEPS — `OptionTransformTypes` (Map/Filter/Calculate/Lookup/Aggregate); (2) field-level transformers — `DataTransformerTypes` (~26 options: Trim/Round/ParseDateTime…); (3) DI-managed transformation SERVICES — `TransformationTypes` (7 ServiceTypeOptions). | `Fdw.Services.Etl.Abstractions/OptionTypes/TransformTypes.cs`, `Fdw.Data.Abstractions/Transformers/DataTransformerTypes.cs`, `Fdw.Services.Transformations/TransformationTypes.cs` |
| **Pipelines & orchestration** | `PipelineStepTypes` + `PipelineTaskTypes` (Extract/Transform/Load/Validate/Branch/Notify; Source/Transform/Destination/Loop/Conditional/Union/ErrorHandler) + `WriteModes` + DataSource/DataDestination kinds; ETL execution + projects (policy inheritance, elevation validation). | `Fdw.Services.Pipelines.Abstractions/TypeCollections/*`, `Fdw.Services.Etl/`, `Fdw.Services.Etl.Projects/` |
| **`[GenerateMapper]` POCO mappers** | ~205 production usages — generated row↔POCO mappers (no reflection at runtime), walking the type incl. its base. | `Fdw.Data.SourceGenerators/PocoMapperGenerator.cs` |
| **Data lineage** | Lineage node/edge/graph model + execution integration. | `Fdw.Data.Lineage/` |

## 5. Configuration

| Capability | What it is | Anchor |
|---|---|---|
| **`[ManagedConfiguration]`** | Class-level attribute on 141 POCOs → generates DDL + FluentValidation validator + UI form + ConfigurationTypes entry. Structural metadata (schema/table/parent FK) is owned by the IDataNode model, not the attribute. | `Fdw.Configuration.SourceGenerators/EmbeddedSources/ManagedConfigurationAttributeSource.cs`, canonical `Fdw.Services.Connections/ConnectionConfiguration.cs` |
| **Parent header + typed body** | Polymorphic configs (Connection→MsSql/PostgreSql/Http/…; SecretManager→…; Notification→…) split into a parent header table (identity + `ServiceOptionType` discriminator + audit/tenant) and one typed-body table per variant. Putting a runtime field on the parent silently fails. | `Fdw.Services.Connections/ConnectionConfiguration.cs`, typed-body dispatch `HttpConnectionType.cs` |
| **`*ConfigurationProvider` (one mechanism)** | ~52 domain providers over one `DefaultConfigurationProvider<TConfig,TCommand>` base that composes the whole aggregate: `Get(name)/Get(id)/Get()` → ComposeTypedBody + ComposeChildren; `Save` cascades owner→children with version-on-write + tag-based cache invalidation. | `Fdw.Services/Configuration/DefaultConfigurationProvider.cs` |
| **`DataStoreProvider` — scoped, tenant-safe** | The DataStore config provider is registered `AddScoped` (not a root singleton): DataStore rows are tenant-scoped (`TenantId`/`VisibilityGroupId` RLS via the scoped `IDataGateway` session context), so a singleton would leak one tenant's DataStore view to every other tenant. Mirrors `DefaultConnectionProvider`'s per-scope lifetime. | `Fdw.Services.Data/DataStoreProvider.cs` |
| **Reader/writer variance split** | `IServiceConfigurationProvider<T>` = covariant reader + contravariant writer composite (FDW-476). | `Fdw.Services.Abstractions/IServiceConfigurationProvider.cs` |
| **App-shipped schema** | `configurationSchema.json` ships with each entry-point app and declares the connections/secret-managers/datastores it needs to reach ConfigurationDb. (ControlDb / `ctrl`+`cfg` dual-source are gone.) | per-app `configurationSchema.json`; `architecture_configuration.md` |

## 6. Service domains

105 `Fdw.Services.*` source directories; 27 are full ServiceTypeCollection domains. Notable:
**Connections, SecretManagers** (MsSql/Sqlite/AzureKeyVault/EnvironmentVariable/UserSecrets),
**Authentication** (OpenIddict), **Authorization** (org-scoped 3-tier RBAC), **Multitenancy**,
**Health monitoring, Users/Credentials, DataVault, Scheduling** (Cron/Interval/Once/Manual triggers),
**Etl** + **Etl.Projects**, **Pipelines, Transformations** (+ DataCleaning), **Calculations**
(formula/windowed/aggregation), **Quality, Workflows, Notifications** (Console/Email/Webhook/System),
**Messaging, SessionState, Settings, Audit, Agents, Resiliency** (Polly/PrimaryBackup/RetryNotify),
**Operations/Execution, Versions, Data (DataGateway)**.

| Capability | What it is | Anchor |
|---|---|---|
| **Auth — OpenIddict stack** | Full OAuth2/OIDC authorization server: RS256 access tokens via `OpenIddictSigningKeyLoader`, DataGateway-backed stores, `{resource}:{action}` permission claims baked at issuance, client-credentials for service-to-service. | `Fdw.Services.Authentication.OpenIddict/` |
| **Authorization (org-scoped RBAC)** | `FdwPermissionRequirement` + `FrameworkPermissionHandler` over baked `{resource}:{action}` claims; `DefaultAuthorizationService` (HasRole/GetRoles/guards); `EffectivePermissionResolver`; org-scoped 3-tier model. | `Fdw.Services.Authorization/` |
| **Multitenancy — a service domain, not middleware** | `MultitenancyTypes` `[ServiceTypeCollection]` with two mutually-exclusive `[ServiceTypeOption]`s: `SingleTenant` (no-op provider) and `Sql` (real Tenant→Org isolation). It's a "declared choice" domain — exactly one option is active per host, selected by the host's `Multitenancy` appsettings row and wired directly via `ByName(...).Configure/RegisterRequiredServices` (not the blanket iterate-every-option path other domains use), because both options register the same `ITenantProvider`/`IOrganizationProvider` interfaces and running both would leave the winner to discovery order. `TenantResolutionMiddleware` (JWT-claim / header GUID-or-slug) sits on top; tenant-discriminated cache keys + connection routing flow from the resolved tenant. | `Fdw.Services.Multitenancy/MultitenancyTypes.cs`, `Fdw.Services.Multitenancy.Sql/Middleware/TenantResolutionMiddleware.cs` |
| **Health monitoring** | `HealthMonitorTypes` `[ServiceTypeCollection]` (`GenerateProvider = true`) — a full service domain, not ASP.NET's built-in health-check middleware: `Local` (in-process) and `HttpClient` (remote-probe) options register factories with `DefaultHealthMonitorProvider`/`IHealthMonitorProvider`; consumers depend on the provider, and which implementation runs is the host's `HealthMonitors` config row (`ServiceOptionType`), resolved at first use. | `Fdw.Services.HealthChecks.Monitoring/HealthMonitorTypes.cs`, `Fdw.Services.HealthChecks.Monitoring/DefaultHealthMonitorProvider.cs` |
| **Scheduling** | `SchedulerTypes` + `TriggerTypes` (Cron/Interval/Once/Manual); background loop dispatches ETL. | `Fdw.Services.Scheduling/` |
| **Execution / Operations** | Execution tracking, state machine, escalation policies, workflow orchestration. | `Fdw.Operations/`, `Fdw.Services.Execution/` |

## 7. Hosting, UI & tooling

| Capability | What it is | Anchor |
|---|---|---|
| **Hosting middleware** | `SecurityHeadersMiddleware`, `WebMcpApiKeyMiddleware` (internal API key), forwarded-headers, bootstrap validation (fail-loud on missing config), bounded Serilog flush on shutdown. | `Fdw.Hosting/` |
| **Render-agnostic UI — Blazor is genuinely on the seam** | `UI.Abstractions` contracts + `IUIRenderer` seam (`Render`/`Prompt`/`RenderPage`), with **3 renderer plugins in the one `UIRenderers` TypeCollection**: `Spectre` (id 1, console), `RazorConsole` (id 2), and **`Blazor`** (id 3, `Fdw.UI.Rendering.Blazor.BlazorUIRenderer`) — mapping the same `IComponentModel`/`IPageModel` to retained-mode `RenderFragment`s via a `FdwComponent`/`FdwRenderHost` dispatcher instead of a blocking console prompt. A conformance suite (`Fdw.UI.Rendering.Conformance.Tests`) feeds the same canonical page models through both Spectre (`TestConsole`) and Blazor (bUnit) and asserts identical `PageResult`/`RenderResult` outcomes for save/cancel/validation-failure/wrong-context-type — proving the two backends are interchangeable behind the seam, not a documentation claim. | `Fdw.UI.Abstractions/Rendering/{IUIRenderer,UIRenderers}.cs`, `Fdw.UI.Rendering.Blazor/BlazorUIRenderer.cs`, `Fdw.UI.Rendering.Conformance.Tests/` |
| **Headless Blazor** | Component triple Context / Provider / ProviderLog — logic lives in render-agnostic providers, skins stay logic-lite. ~69 `*Provider.razor`. | canonical `RoleProvider.razor`, `Fdw.UI.Components/` |
| **Realtime hubs + per-org firehose** | `RealTimeHubBase<TClient>` + `RealTimeHubs` `[TypeCollection]` (Register/MapRealTimeHubs) — 4 SignalR hubs as one `[TypeOption]` each (replaces the per-app `AddFrameworkSignalR`); authentication is mandatory (`[Authorize]` on the base + `RequireAuthorization` always applied when mapping — no hub can be anonymous). Pipeline status broadcasts are **org-scoped**: `PipelineStatusHub.OnJoin` joins `org:{orgId}:pipeline-updates` from the caller's `org_id` JWT claim (no claim ⇒ no firehose join, never a placeholder org), and the broadcaster targets that group from the pipeline's owning `OrgId` — replacing the old unconditional cross-org `pipeline-updates` group. A connection in one org never receives another org's pipeline stream; `pipeline:{name}`/`execution:{id}` subscriptions remain unaffected. | `Fdw.SignalR/RealTimeHubBase.cs`, `Fdw.SignalR/RealTimeHubs.cs`, `docs/REALTIME-HUB-PATH.md` |
| **VS Code shell** | Ship a VS Code extension by writing a .NET host — zero TS/npm for consumers. | `architecture_vscode_shell` |

---

## Sources & caveats

- Counts grepped from `fractaldataworks/public/src` (excluding `obj/`), 2026-07-02, for the 1.0.1-rc.1 release. Exact: FDW001–FDW023 (23 IDs), 40 analyzer classes, 9 `*.SourceGenerators` projects (~17 `IIncrementalGenerator` classes across 8 of them; `Fdw.SourceGenerators` is a shared builder library, not itself a generator host), 6 connection backends, 27 ServiceTypeCollections (11 with `GenerateProvider = true`), 11 `ResultCategories`, 141 `[ManagedConfiguration]`, 301 TypeCollection files / ~2260 TypeOptions. Approximate (generator/scoping noise): ~560 ResultCodes across ~52 `*ResultCodes` collections and ~52 domain ConfigurationProviders, 300+ MessageLogging methods, ~26 field transformers, ~205 `[GenerateMapper]` production usages.
- This is a first-release capability catalog, not a changelog — it describes what FDW 1.0.1-rc.1 *is*, for a reader meeting it cold. The dev guide (60 chapters) is the narrative companion; it is being truth-upped chapter-by-chapter and lags this catalog on newer subsystems (health monitoring, multitenancy-as-domain, the Blazor renderer, per-org firehose).
- How to keep this honest: every claim here is grepped/read against source. When a subsystem changes materially, re-verify the affected section and bump the date. Do not copy numbers forward blindly.
