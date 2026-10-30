# Changelog

All notable changes to the FractalDataWorks framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.4] - 2026-10-30

Released with reference-servicetypes 1.4.4 and the reference servers and console 1.4.4. An application with its own configuration database needs the schema changes listed under Breaking applied before it takes this version. Test-only fixes are not listed, and each entry under Fixed, and each entry that begins Fixed:, describes the fault as it was before 1.4.4; 1.4.4 fixes it.

### Breaking

- The promotion API is removed: every `/promotion` route (environments, requests, approve, reject, compare and execute) answers 404, and the promotion tables in the quality schema, their configuration classes and providers, and the hosts' declarations for them are dropped.
- `PATCH /secret-managers/{name}` takes `{"configuration": {...}}`, the type's own settings, merges them over the stored ones and saves them. A setting the type does not take is refused by name. `Description` and `Environment` are gone from the request; they were never saved.
- `ConditionOperator` is no longer part of the notification rule create API. A rule's severity is one of those `GET /notifications/rules/severities` serves, checked when the rule is saved, with no default.
- `AuiMiddleware` builds the manifest for the signed-in user and answers 401 when nobody is signed in. The reference-aui demo host wires the middleware without authentication, so its manifests answer 401 once it moves to this build.
- The OpenAPI, OData and SQL Server schema importers now register in `SchemaImporters`. The OpenAPI importer refuses a field with no type or no converter (`OpenApiFieldTypeUnknown`) and imports a plain integer as Int64, where it used to import both as string. The OData importer refuses `$metadata` with no `EntityType`.
- `GET /secret-manager-types` requires the platform administrator and names each type's settings container. The reference API references all six secret manager Registration packages, including the new `FdwReferenceSecretManagers.HashiCorpVault.Registration`.
- `GET /search` and `POST /find` are removed from the API.
- Fixed: Four DELETE routes answer 204 for an id that does not exist (annotation by id, glossary term, quality rule, escalation policy).
- Pipeline runs are no longer one at a time across pipelines. The ETL server runs the runs of different pipelines at once, up to `MaxConcurrentPipelineRunsPerServer` and `MaxConcurrentPipelineRunsPerTenant`; runs that write the same table take turns, and a run whose writes cannot be told runs alone. Neither setting has a value in code: an environment whose configuration lacks either row refuses every pipeline run, naming the setting, so every environment needs both before it takes 1.4.4.
- Each pipeline states how many of its runs may run at once (`MaxParallelRuns`, at least 1) and what happens to a run triggered while that many are active (`OverflowBehavior`: `Queue`, the run waits its turn, or `Refuse`, the run ends as failed with reason `RunRefused`.
- A calculation whose DataSet input names a view (a data set made from another data set plus a saved filter, such as AfcQuarterbacks, AfcPlayers filtered to Position = QB) reads that view's rows with its saved filter, as the caller, up to the row cap (`MaxCalculatedFilterRows`), by the same path the `/data` routes use, with rows cut to the data set's declared fields.
- `POST /calculation-entities` and `PATCH /calculation-entities/{id}` answer 400 (ENDPOINTS-21539) for a configuration member the calculation kind does not declare. Formula declares `formulaBody`, `formulaLanguage` and `timeoutSeconds`; Windowed declares `targetField` and `windowFunction`.
- The ETL host's trigger queue size, 100 before, is now the server setting `MaxQueuedPipelineTriggers` (Int32, positive), read once when the host starts. It has no default in code: any host that runs the pipeline trigger queue and whose server settings lack the row does not start, and its log names the setting.
- A calculation whose input is a data set reads that data set's rows as the caller: the data store applies the caller's row-level security through the request's gateway, up to the calculated-filter row cap (the `MaxCalculatedFilterRows` setting for the caller's tenant and role, as `/data` resolves it).
- A pipeline that belongs to the global tenant now reads every tenant's rows, read only, except the system tenant's and rows marked Restricted. Before, the database's row filter left the global tenant out of a run, so such a pipeline over another tenant's data set was accepted and then failed at run time with "Dataset not found"; that reason now names the tenant cause.
- Fixed: PATCH /calculation-entities/{id} with part of a configuration resets the members left out to their defaults.
- `IDomainConfigurationProvider<T>` (Fdw.Services.Abstractions) gains `Update`, typed and untyped: the same write as `Save` except that the record must name an existing member, and it fails as not found (event 31061, UpdateTargetNotFound) when the member's row is gone, or when the record carries an Id that names no member.
- How often a host checks each configuration database for a reset is now the server setting `ResetWatchIntervalSeconds` (Int32, positive), read once when the host starts. It has no value in code: a host that registers settings and finds no positive row does not start, and its log names the setting (event 20988). databases-seed carries it with 10, so a reseeded environment has it.
- Removed with it: `POST /calculation-entities/validate-formula`, `ICalculationApiClient.ValidateFormula` and `GetFunctions`, the Formula step executor, the Fdw.Expressions package, the Formula editor client types and the console's FormulaTokenizer; CALCULATIONS-21024 and CALCULATIONS-21025 are retired.
- Pipeline wait thresholds are new configuration, and every configuration database a host of this version reads must carry it before the host starts: three tables, `pipe.WaitThresholdLevel` (one row per owner that states a level: the server, a pipeline or a schedule, with its mode Inherit, Own or None), `pipe.WaitThreshold` and `pipe.WaitThresholdRule`, declared in every host's `configurationSchema.json`, and the server's level row, which databases-seed writes with mode None (no alert, today's behaviour).
- A pipeline run that has not started (held for a delayed start, waiting its turn, or waiting for approval) is kept in the new operational table `etl.PipelineRunRequest` and survives an ETL host restart; its run values are stored as they are, because a run value is never a secret.
- Each kind of calculation input says which members it takes, and the type-collection values route lists them as its parameters: a DataSet input takes `dataSetName`; a Scalar input takes `scalarValueType` and `scalarSerializedValue`; a Container input takes `connectionName` and `containerPath`.
- `POST /calculations/execute` finds a data set name the way `/data` does, so the name of a view is found; an unknown name still answers 404. `ExecuteCalculationEndpointBase`'s protected constructor takes `IDataSetResolver` in place of `IConfigurationGateway`, so an endpoint that derives from it must pass the resolver (the reference `ExecuteCalculationEndpoint` does).
- A calculation create or update refuses an input that leaves out a member its kind requires, with 400 ENDPOINTS-21548 naming the input's alias, its kind and the member: a DataSet input needs `dataSetName`, a Container input `connectionName` and `containerPath`, a Scalar input `scalarValueType` and `scalarSerializedValue`, each named on its own.
- A Scalar input's value is checked against its type when a calculation is created or updated: a value that is not a value of its type is refused with 400 ENDPOINTS-21549, "Calculation input '<alias>' has scalar value '<value>', which is not a value of type '<type>'".
- A pipeline destination states what it writes, as a source does: `targetKind` File writes one file and Files writes one file per value of the destination's split field, each holding only that value's rows, with the required `truncateFirst` deciding whether an existing file is replaced or added to.
- A connection or secret manager that states no command timeout no longer gets 30 seconds from the code or the column: it takes the required server setting `DefaultCommandTimeoutSeconds`, and is not built where neither is present (SETTINGS-62030 to 62033 name it). The SQL Server, PostgreSQL and SQLite connection columns and the SQL Server and SQLite secret manager columns lose their `DEFAULT 30`. A host's bootstrap connections state their own `CommandTimeoutSeconds` in `configurationSchema.json`.
- A create that used to overwrite a member of the same name now refuses it with a conflict (SERVICES-40001, CreateTargetExists): CreateRole, CreateDataSetAnnotation, CreateQualityRule, CreateEscalationPolicy and BindExternalIdentity. A caller that relied on a create replacing the old member must use the update.

### Added

- The path setting of a container source and a container destination is declared as a data store path, so a client can offer the store's own paths.
- A person who belongs to more than one organization marks one as default, and a sign-in issues a token already acting in it.
- GET /secret-manager-types tells the console which settings can be emptied (canBeEmpty), by the same rule the PATCH null refusal uses.
- A person's dark mode is kept as light, dark or follow-my-computer, and a person who never chose reads as not chosen.
- A for-each step works through its list in batches: a batch size that is a pipeline default with a per-run override.
- A pipeline load target is picked from a type collection: a container, a data set, a file, or an array of files.
- A further run of the same pipeline waits its turn rather than being refused, up to the pipeline's own limit on parallel runs (`MaxParallelRuns`).
- The console shows which pipeline runs are running and which are queued, and why a queued run waits.
- A pipeline can delay a run triggered past its limit: start after a set delay, or at a set time.
- The list of file names of the Files kind may come from a variable in scope, such as the batch of a For each.

### Fixed

- Cancel, pause and resume only change a run's record: the running pipeline never sees them, so a cancelled run keeps running.
- OrchestrationExecutor reports a step as succeeded when no step executor is registered and the step did nothing.
- GET /configuration/types/detail answers no settings for any type, so a secret manager's settings form cannot be built.
- Secret managers: a failed list read answers an empty list, a description is taken and dropped, and no route reads a manager's settings back.
- Pipeline run history answers only success, so a cancelled run or one still going reads as failed.
- Which configuration members are secret is decided by a name pattern, so a name is masked and a Token is not.
- ETL project and node reads answer 500: container 'OrchestrationNode' declares no foreign key to 'OrchestrationNodeConfiguration'.
- POST /identities/{name}/verify with an audience answers a bare gateway 502 instead of 200 with succeeded:false.
- Calculation execute answers 200 for an unknown calculation type and for a windowed calculation that failed.
- The request validators of the fdw endpoint base assemblies are never registered, so their rules are inert (calculation execute accepts an unknown type).
- PATCH /secret-managers/{name} answers 200 for a setting sent as null and keeps the old value.
- A data set's note cannot be deleted from what the API answers (delete takes an id no read gives), and "resolve" saves the note unchanged.
- POST /catalog/annotations/{AnnotationId}/resolve saves the note unchanged and answers it as resolved.
- Endpoints tell a not-found failure apart by searching its message for "not found".
- Deleting a secret manager is still allowed while notifications, authentication flows, data vaults, client credentials, token managers, hosts and Aegis commands name it.
- After a stage is renamed, the project read lists the stage twice: the old version and the new one.
- A save of a record carrying member A's Id under member B's name is written into B without a word.
- GET /users/me/preferences answers the empty shape for a person who has stored preferences.
- A calculation entity never receives the rows of its DataSet input, so Windowed and Formula entities run over zero rows and answer success.
- POST /calculation-entities/get answers no inputs, outputDataSetName or resultFieldName, and a failed read as 404.
- Creating or updating a calculation drops a Scalar input's value and accepts an unknown input kind.
- Two overlapping runs of a pipeline that has no watermark yet: the second to finish fails with WatermarkWriteFailed after its load committed.
- POST /calculation-entities/get does not return the calculation's configuration, so what create or PATCH set cannot be read back.
- POST /calculations/windowed answers partitionCount 0 and rowCount 0 beside a resultJson that holds the real counts.
- ProjectExecutionQueue and OrchestrationNodeExecutionQueue drop a trigger past their capacity and report it queued.
- The refusal ENDPOINTS-21532 for a type change without a configuration answers the generic "The request was invalid".
- The unreadable-configuration refusal ENDPOINTS-21531 shows System.Text.Json internals to the caller.
- PATCH /calculation-entities/{id} with a different calculationEntityType answers 200 and changes nothing.
- Through the API, a trigger a wait threshold stops answers a bare 503 and a refused pipeline delete a generic 409, without the reason, the threshold or its owner.
- GET /type-collections/ComparisonOperators/values and /AlertSeverities/values answer 404, so no wait threshold can be given a comparison or severity from the console.
- The 503 a stopped trigger answers through the API carries no executionId, so the stopped run's record cannot be read from it.
- A pipeline run cut off by its connection's command timeout fails with "SQL query timeout", not naming the connection or the seconds; a failure with no coded message now says so and lists what it carried.
- A pipeline run that was running when the ETL host stopped is still marked Running after the host starts again.
- deleting a pipeline or schedule that has its own wait thresholds, or that another owner's threshold counts, makes the queue refuse every run.
- The no-input-rows failure tells a person to give a calculation a Scalar input, which cannot help.
- A stop-new-runs threshold saved while its condition already holds lets one more run through.
- A pipeline Update load into SQL Server or PostgreSQL finds no field to update, because the update translators read a record's properties and the load sends a dictionary.
- An Update with no filter whose record does not carry the primary key builds UPDATE with no WHERE clause in the SQL Server, PostgreSQL and SQLite translators.
- Fixed: The SQL Server secret manager's CommandTimeoutSeconds limits none of its lookups: its commands run through the gateway with the data store connection's timeout.
- A data command that carries its own command timeout and expands into simpler commands runs its steps without that timeout, and is not refused.

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
