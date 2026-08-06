# Polymorphic Configuration Pattern

When a service domain has **multiple type variants** with **different runtime field shapes**, FDW models the configuration as a **parent header table** plus one **typed-body table per variant**. The parent holds identity; each typed body holds the runtime configuration for that variant. The dispatch picks a variant by `ServiceOptionType` discriminator and the variant's factory consumes only the typed-body fields.

This is the **polymorphic configuration pattern**. It is the official FDW pattern for any service domain that has — or could grow to have — more than one specialization.

## When the pattern applies

A domain needs polymorphic configuration if and only if **both** are true:
1. It has 2+ `[ServiceTypeOption]` impls under one `[ServiceTypeCollection]`.
2. The variants have **different runtime field shapes** (not just different values for the same fields).

| Situation | Pattern |
|---|---|
| Single TypeOption — one variant only | Single flat table. No typed body. |
| Multi-variant, same field shape, different values | [Pattern A — typed columns](../src/Fdw.Configuration.SourceGenerators/README.md). One table. |
| Multi-variant, **different field shapes** | **This pattern.** Parent + typed body per variant. |
| Open-set extension via name/value pairs | Pattern C — Properties (see internal `configuration-properties-pattern` skill in `claude-tools`) |

The placement rule below applies to every domain that follows this pattern.

## The placement rule

The runtime call path is:

1. **Domain provider** (`ConnectionConfigurationProvider`) loads the parent header by name or id → a `ConnectionConfiguration` populated from parent columns only.
2. **The same domain provider** then runs `PopulateTypedBody`: it reads `header.ServiceOptionType`, looks up the registered typed provider for that discriminator, calls that provider's `Get(header.Id)`, and assigns the result to `header.Configuration`. Dispatch is internal to the domain provider — it is **not** done by a `DefaultServiceProvider`.
3. **Typed provider** queries the typed-body table by `[Id] = @parentId` → returns the variant configuration (`MsSqlConnectionConfiguration`, etc.) populated from typed-body columns only.
4. **Factory** builds the runtime service from the typed config (`header.Configuration`).

**Step 4 is the rule:** the factory receives the **typed** configuration object only. The parent header carries identity and the dispatch discriminator; everything runtime lives on the typed body.

> **Whatever the factory needs at runtime must be on the typed body.**

If a runtime field lives on the parent header, the factory will see it as the type's default value — usually empty/null — and the service will fail to construct.

## Schema split

### Parent (header) — identity-only columns

The parent POCO declares only identity and dispatch properties. Version-on-write, tenant/RBAC, and audit **columns are added by the DDL generator** — they are not properties on the POCO.

| POCO property | Required | Notes |
|---|---|---|
| `RowId` | yes | version-specific PK (`NEWSEQUENTIALID()`) |
| `Id` | yes | durable logical identity |
| `Name` | yes | human/UI lookup key |
| `ServiceOptionType` | yes | discriminator — drives dispatch |
| `Description` | optional | UI-only |

Generator-added columns (not on the POCO): `IsCurrent` / `IsDeleted` (version-on-write), `TenantId` / `VisibilityGroupId` (tenant scope), and the audit set (`SrcCreateDate`, `CreateDate`, `CreateBy`, `CreateOnBehalfOf`, `ModifyDate`, `ModifyBy`, `ModifyOnBehalfOf`).

Nothing else. **If a field is read at runtime to construct or run the service, it does not belong on the parent.**

### Typed body (`<TypeName><Domain>`) — runtime columns

- `RowId` (version-specific PK)
- `Id` — the typed body's own logical identity (distinct from the parent's `Id`)
- `<Parent>Id` — FK to the parent's logical Id; the typed-body provider filters on this (e.g. `WHERE [ConnectionId] = @parentId AND IsCurrent = 1`)
- All runtime fields the factory needs to construct the service.

### Decision tree

When you add a column to a `[ManagedConfiguration]` POCO that's part of a multi-type domain:

```
Is the column needed by the factory at runtime?
├── No (UI/admin only)        → put it on parent
└── Yes (factory reads it)
    ├── Universal across types → duplicate on every typed body
    └── Variant-specific       → put it on the relevant typed body only
```

Duplicating the same column across N typed-body tables is the correct trade-off when N variants share a runtime field. Conflating identity and runtime by leaving the field on the parent breaks the load-and-dispatch contract.

## Reference: Connection family (gold standard)

The `ConnectionConfiguration` parent POCO declares only: `RowId, Id, Name, SectionName, ServiceType, ServiceOptionType, Description, Environment, LastTestedAt, LastTestSuccess, LastTestMessage, DiscoveryEnabled`, plus the `[NotMapped]` `Configuration` slot that holds the loaded typed body. Version/tenant/audit columns are added by the DDL generator.

| Table | Role | Columns |
|---|---|---|
| `conn.Connection` | Parent (identity) | `RowId, Id, Name, SectionName, ServiceType, ServiceOptionType, Description, Environment, LastTestedAt, LastTestSuccess, LastTestMessage, DiscoveryEnabled` + generator-added version/tenant/audit |
| `conn.MsSqlConnection` | Typed body | `RowId, Id, ConnectionId, Server, Database, Port, InstanceName, …Authentication…, …Pool…` + generator-added |
| `conn.PostgreSqlConnection` | Typed body | `RowId, Id, ConnectionId, Host, Database, Port, …` + generator-added |

Each concrete connection type gets its own typed-body table with an identity-only parent. Each variant's factory (`MsSqlConnectionFactory.Create`, …) consumes only its own typed-body fields.

**Inheritance note (important):** Connection typed bodies do **not** inherit `ConnectionConfiguration`. `MsSqlConnectionConfiguration` is a standalone POCO that implements the marker interface `IConnectionConfiguration` and links to its parent via the `ConnectionId` FK. (Contrast with the Pipeline family below, which *does* use real base-class inheritance.) The typed body is assigned onto the parent's `Configuration` property at load time, not merged into one row.

`MsSqlConnectionConfiguration.Server` and an HTTP connection's `BaseUrl` play similar conceptual roles ("where to connect") but have different shapes — that's why they're typed-body specializations rather than typed-column variants.

**JSON dispatch:** when a `ConnectionConfiguration` round-trips through System.Text.Json, the variant is resolved by a custom `ConnectionConfigurationJsonConverter` keyed on the `ServiceOptionType` discriminator — **not** by `[JsonPolymorphic]`/`[JsonDerivedType]` attributes. The converter reads `ServiceOptionType`, resolves the concrete type from `ConnectionTypes` (populated by module initializers at assembly load), and deserializes the nested `Configuration` body into it. Attributes are deliberately avoided because the typed-body types live in packages that `Services.Connections` cannot reference.

## Reference: Pipeline family (base-class inheritance variant)

The Pipeline family demonstrates the **inheritance** form of this pattern. `EtlPipelineConfiguration` is the identity-only anchor; the typed bodies inherit from it directly:

- `BatchCopyPipelineConfiguration : EtlPipelineConfiguration` (`ServiceType = "BatchCopy"`)
- `StreamingPipelineConfiguration : EtlPipelineConfiguration` (`ServiceType = "Streaming"`)
- `ConnectorSourcePipelineConfiguration : EtlPipelineConfiguration` (`ServiceType = "ConnectorSource"`)

(All three live in `Fdw.Services.Etl`.) Unlike the Connection family — where the typed body is a standalone POCO referenced via `Configuration` — here each variant is a subclass and the DDL generator emits a separate child table per subclass.

The anchor `EtlPipelineConfiguration` declares only identity/dispatch properties: `Id, Name, ServiceType, ServiceOptionType, SectionName`, plus the `Transforms` child collection (populated after hydration, never a column).

The variant subclasses carry the runtime fields the factory reads. Shared columns are repeated on each subclass; variant-specific columns appear only where relevant:

| Class (→ generated table) | Declared runtime properties |
|---|---|
| `BatchCopyPipelineConfiguration` | `PipelineId, IsEnabled, SourceConnectionName, SourceDataSet, DestinationConnectionName, DestinationDataSet, BatchSize, ContinueOnError, MaxErrors, SecretManagerName, SecretKeyName, ResiliencyPolicyName, SourceDataSetId, SourceDataSetRowId, SinkDataSetId, SinkDataSetRowId, PipelineVersion, MaxParallelism, LoadMode, TruncateBeforeLoad` |
| `StreamingPipelineConfiguration` | `PipelineId, IsEnabled, SourceConnectionName, SourceDataSet, DestinationConnectionName, DestinationDataSet, BatchSize, ContinueOnError, MaxErrors, SecretManagerName, SecretKeyName, ResiliencyPolicyName, SourceDataSetId, SourceDataSetRowId, SinkDataSetId, SinkDataSetRowId, PipelineVersion, BufferSize, FlushIntervalMs, UseWindowing, WindowDurationSeconds, MaxRecordsPerSecond` |
| `ConnectorSourcePipelineConfiguration` | `IsEnabled, ConnectorName, Variables, DestinationConnectionName, DestinationDataSet, BatchSize, ContinueOnError, MaxErrors, SecretManagerName, ResiliencyPolicyName, PipelineVersion` |

The variant-specific columns (`MaxParallelism`/`LoadMode`/`TruncateBeforeLoad` for batch; `BufferSize`/`FlushIntervalMs`/… for streaming; `ConnectorName`/`Variables` for connector-source) live only on the relevant subclass. Confirm exact table/column layout against the generated DDL rather than memorizing this list.

## Auditing an existing domain

A domain is **broken** in this respect if both:
- Multiple `[ServiceTypeOption]` impls exist, AND
- The parent table contains columns that any factory reads at runtime.

Symptom at runtime: the factory log shows `Configuration loaded: ''` (empty Name / source / destination), and the runtime fails with `Container '' not found in configuration` or similar empty-config errors.

Quick check on the parent table:

```bash
sqlcmd -d ConfigurationDb -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='<schema>' AND TABLE_NAME='<Domain>'"
```

Anything beyond the identity-column list above is suspect — confirm by tracing the factory's `Create` method.

### Two valid resolutions

When a domain has runtime fields on the parent, there are two valid migrations depending on what the runtime actually does:

1. **Parent is dispatch-only, runtime reads typed body** — move the parent runtime columns into all typed-body tables (duplicate where shared). This is the canonical Connection/Pipeline shape.
2. **Runtime reads from parent and typed bodies are dead** — drop the typed-body tables. The domain doesn't actually need polymorphic config; its variants differ in shape only on paper. (Schedule was an example — `sched.CronSchedule` and `sched.IntervalSchedule` were never queried at runtime; `CronExpression` and `IntervalSeconds` lived on `sched.Schedule` and `DefaultSchedulingService` read them directly.)

Pick (2) when an audit confirms the runtime never reaches the typed-body tables. Pick (1) when the runtime dispatches via typed-body Get, which is the standard FDW path.

## Single-type domains

A `[ServiceTypeCollection]` with a single `[ServiceTypeOption]` impl does not need a typed body. Use one flat table; runtime fields live on the parent. This is acceptable until a second variant is added; at that point migrate to polymorphic configuration.

## Related skills and patterns

The following internal Claude skills (in `claude-tools/`, not shipped publicly) cover the implementation steps:

- `parent-vs-typed-body-fields` — placement rule reference
- `add-typed-body-chain` — adds a new typed-body chain when expanding to a new variant
- `add-managed-config-columns` — adds columns within an existing typed body (Pattern A)
- `configuration-properties-pattern` — open-set extension via `Properties` KVP children (Pattern C)

## Related Documentation

- [ManagedConfiguration](03-01-ManagedConfiguration.md) — attribute and DDL conventions
- [Configuration Provider Registration Pattern](03-05-Configuration-Provider-Registration-Pattern.md) — DI registration for parent + typed providers
- [TypeCollections Overview](04-01-Overview.md) — `[ServiceTypeCollection]` and `[ServiceTypeOption]`
- [Service Domains Overview](06-01-Service-Domains-Overview.md) — how factories consume configuration
- [Connections Service Domain](06-03-Connections-Service-Domain.md) — gold-standard implementation
