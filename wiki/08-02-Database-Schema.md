# Database Schema

## Physical databases

FDW deploys three physical databases by convention:

| Database | Role |
|---|---|
| **ConfigurationDb** | All framework configuration — connections, datastores, datasets, pipelines, schedules, auth, themes, etc. Single source of runtime configuration. |
| **AuthDb** | Authentication state — user credentials, sessions, refresh tokens. Isolated for security. |
| **OpsDb** | Operations and observability — execution history, health snapshots, audit log, scheduling state. Isolated to avoid lock contention with configuration reads. |

A fourth database holds reference / business data (e.g. `DataDb` with the `NflData` schema
in the reference repos). It is registered as a runtime Connection inside ConfigurationDb,
not a framework-managed database.

## ConfigurationDb schemas (one per domain)

ConfigurationDb is organised by **service category** — every `[ManagedConfiguration]` type
lives in its category's schema and is writable through that category's
`IConfigurationWriter<T>`. There is no read-only schema and no `IsSystem` flag.

| Schema | Owns |
|---|---|
| `auth` | Authentication configuration (JWT, API key, OAuth, etc.) |
| `authz` | Authorization policies, role mappings |
| `agent` | Agent service definitions and action types |
| `audit` | Audit-event templates and retention policies |
| `calc` | Calculation expressions and caches |
| `catalog` | Promotion catalog and lineage entries |
| `conn` | Connection definitions (per polymorphic pattern: parent header + typed body per variant) |
| `data` | DataStores, DataPaths, DataContainers, DataFields, Keys, DataSets |
| `notify` | Notification channels (email, webhook, console) |
| `pipe` | ETL pipelines and pipeline steps |
| `quality` | Quality rules and assessments |
| `sched` | Schedules and triggers (Cron, Interval, Once, Manual) |
| `sec` | Secret-manager configurations |
| `settings` | Tenant-scoped key/value settings |
| `tenant` | Tenant + tenant feature definitions |
| `transform` | Transformation operators (calculation, aggregation, pivot, lookup) |
| `usr` | User profile + preference rows |
| `workflow` | Workflow definitions and escalation policies |

The complete list of tables and DDL lives in the `databases/` repo (`DATABASE-MAP.md` for
the current authoritative inventory).

## OpsDb schemas

| Schema | Owns |
|---|---|
| `ops` | Execution tracking — `ExecutionEvent`, `ExecutionItem`, `WorkflowExecution`, `ConnectionHealthCheck`, `ConnectionLimitCounter` |
| `etl` | Pipeline execution rows (`PipelineExecution`) |
| `sched` | Schedule execution rows (`ScheduleExecution`) |
| `dq` | Data-quality rule execution rows (`QualityRuleExecution`) |
| `msg` | Messaging / outbox — `Message`, `MessageRecipient`, `AccessRequest` |

## AuthDb schema

A single `auth` schema with credential, session, and refresh-token tables. Isolated DB so
auth writes don't contend with configuration reads and so credential storage can have its
own backup / encryption policy.

## DDL conventions

- One DDL file per table, in `databases/<DbName>/<schema>/<TableName>.sql`.
- Comma-before column lists; no square brackets; column types and nullability aligned.
- FK constraints reference `{Parent}RowId` (not `{Parent}Id`) — every PK is
  `NEWSEQUENTIALID()` so child rows pin to a specific parent version.
- Version-on-write audit columns (`IsCurrent`, `IsDeleted`, `SrcCreateDate`, `CreateDate`,
  `CreateBy`, `CreateOnBehalfOf`, `ModifyDate`, `ModifyBy`, `ModifyOnBehalfOf`).
- Tenant / RBAC columns (`TenantId`, `VisibilityGroupId`) where applicable.

## Deployment

The `databases` repo is the source of truth; FDW does not contain `.sqlproj` files. Each
database has its own DACPAC build and seed scripts. Reference repositories
(`reference-api`, `reference-etl`, `reference-scheduler`, `reference-ui`) do **not** define
database projects — they consume ConfigurationDb / AuthDb / OpsDb as already deployed.

## Logins

Each database has dedicated SQL logins with least-privilege schema grants. Passwords are
resolved through `EnvSecrets` (`FDW_SECRET_*` environment variables). The reference
deployment uses:

| Login | Database | Grants |
|---|---|---|
| `fdw_config` | ConfigurationDb | CRUD on all domain schemas |
| `fdw_config_ro` | ConfigurationDb | SELECT only (for read-replica scenarios) |
| `fdw_auth` | AuthDb | CRUD on `auth` |
| `fdw_ops` | OpsDb | CRUD on all schemas |
| `fdw_etl` / `fdw_sched` / `fdw_tenant` | various | service-specific |
| `fdw_nfl` | DataDb | CRUD on `NflData` only |

See `databases/DATABASE-MAP.md` for the current matrix.

## Related

- [JSON-Driven Configuration Startup](03-05-JSON-Driven-Configuration.md)
- [ManagedConfiguration](03-01-ManagedConfiguration.md)
- [Configuration Writers](03-02-ConfigurationWriters.md)
- [Docker & Database Setup](17-02-Docker-And-Database-Setup.md)
