# Database Setup

This guide covers FDW's database projects, schema-deployment workflow, and the per-schema SQL logins used at runtime.

## Repository Layout

Database projects live in the `databases` repository (not in the FractalDataWorks repo). Each database has its own SQL Server Database Project (`Microsoft.Build.Sql` SDK):

| Project | Database | Schemas |
|---------|----------|---------|
| `ConfigurationDb/ConfigurationDb.sqlproj` | ConfigurationDb | `auth`, `authz`, `conn`, `data`, `pipe`, `sched`, `quality`, `catalog`, `calc`, `notify`, `settings`, `sec`, `transform`, `workflow`, `agent`, `audit`, `usr`, `tenant` |
| `OpsDb/OpsDb.sqlproj` | OpsDb | `ops`, `etl`, `sched`, `msg`, `dq` |
| `AuthDb/AuthDb.sqlproj` | AuthDb | runtime auth (credentials, sessions, tokens) |
| `DataDb/DataDb.sqlproj` | DataDb | tenant data (e.g., NflStats hosted at VM 105) |
| `NexusDb/NexusDb.sqlproj` | NexusDb | nexus-track / nexus-vcs |

## Building a Dacpac

Each project builds to a dacpac via the standard .NET build:

```bash
cd databases/ConfigurationDb
dotnet build -c Release
# Output: bin/Release/ConfigurationDb.dacpac
```

## Deployment

Dacpacs deploy via `sqlpackage /Action:Publish`. The repo includes per-database security and seed scripts:

- `databases/<Db>/security/permissions.sql` — creates the schema-scoped user(s) and grants
- `databases/ConfigurationDb/seed/*.sql` — startup seed (e.g., `02-seed-cfg-runtime-config.sql` registers OpsDb and the EnvSecrets secret manager)

The expected deployment sequence is:

1. Publish ConfigurationDb dacpac
2. Publish OpsDb dacpac
3. Publish AuthDb dacpac (and DataDb on the data-hosting server)
4. Run ConfigurationDb seed scripts (registers OpsDb / AuthDb / DataDb as connections in `conn.Connection`)
5. Start FDW services (Api, ETL, Scheduler, UI)

## Schema-Specific Logins

FDW uses least-privilege SQL logins. Each schema has a dedicated login with full DML on its own schema and SELECT on read-only dependencies:

| Login | Database | Schemas (R/W) | Schemas (RO) | Secret env var |
|-------|----------|---------------|--------------|----------------|
| `fdw_config` | ConfigurationDb | all `conn`/`data`/`pipe`/`sched`/`sec`/etc. configuration schemas | — | `FDW_SECRET_CONFIG_PASSWORD` |
| `fdw_config_ro` | ConfigurationDb | — | configuration schemas | `FDW_SECRET_CONFIG_RO_PASSWORD` |
| `fdw_auth` | ConfigurationDb / AuthDb | — | `auth` | `FDW_SECRET_AUTH_PASSWORD` |
| `fdw_tenant` | ConfigurationDb | — | `tenant` | `FDW_SECRET_TENANT_PASSWORD` |
| `fdw_ops` | OpsDb | `ops`, `etl`, `sched`, `msg`, `dq` | — | `FDW_SECRET_OPS_PASSWORD` |
| `fdw_nfl` | DataDb | NflStats schemas | — | `FDW_SECRET_NFL_PASSWORD` |

Permissions are emitted by per-database `security/permissions.sql`. Example from `OpsDb/security/permissions.sql`:

```sql
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::ops   TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::etl   TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::sched TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::msg   TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dq    TO fdw_ops;
```

## Table Conventions

All configuration and ops tables follow the version-on-write pattern:

- **`RowId`** (`UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()`) — version-specific PK
- **`Id`** (`UNIQUEIDENTIFIER`) — durable logical identity (root tables only)
- **`{Parent}Id`** — FK to parent's logical `Id` (child tables only)
- **`IsCurrent`** / **`IsDeleted`** — soft-delete + version flags
- **Filtered unique indexes** — `WHERE IsCurrent = 1 AND IsDeleted = 0` enforces "one current per entity"

See `databases/DATABASE-MAP.md` in the `databases` repository for the authoritative table inventory.

## Connection Registration

Once the dacpacs are deployed, runtime connections (OpsDb, AuthDb, DataDb, user-defined connections) are registered as rows in ConfigurationDb's `conn.Connection` table (parent) + typed body (`conn.MsSqlConnection`, `conn.PostgreSqlConnection`, etc.) + authentication body (`conn.MsSqlConnectionAuthentication`, etc.).

The only startup connection that ships in code rather than the database is **ConfigurationDb itself**, declared in `configurationSchema.json` shipped with each entry-point app and loaded into `IConfiguration` at the very start of `Program.cs`.

## Environment Secrets

All service login passwords resolve from `FDW_SECRET_*` environment variables via the `EnvSecrets` secret manager (seeded by default). The seed lives at `databases/ConfigurationDb/seed/02-seed-cfg-runtime-config.sql`. The `EnvironmentVariableSecretManager` strips the `FDW_SECRET_` prefix to match `SecretKeyName` values referenced by connection-authentication and JWT-authentication configurations.

For Azure deployments, replace `EnvSecrets` with `AzureKeyVault` per [12-10-Secret-Management.md](12-10-Secret-Management.md).

## Related Documentation

- [08-02 Database Schema](08-02-Database-Schema.md) — table inventory and conventions
- [12-02 Deployment Guide](12-02-Deployment-Guide.md) — production deployment
- [12-13 OpsDb Configuration](12-13-OpsDb-Configuration.md) — OpsDb specifics
- [12-10 Secret Management](12-10-Secret-Management.md) — `FDW_SECRET_*` and secret-manager wiring
