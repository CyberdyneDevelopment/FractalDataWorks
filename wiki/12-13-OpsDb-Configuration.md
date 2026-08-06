# OpsDb Configuration

Operational runtime data (execution tracking, scheduling, messaging, data quality) lives in a dedicated **OpsDb** database, separate from ConfigurationDb. This page explains why the separation exists, how to register OpsDb as a connection, and the deployment requirements.

## Why a Separate Database

The OpsDb schemas are written by every pipeline execution, schedule tick, workflow step, and quality check. In a busy environment these are high-frequency, high-volume writes:

- Every pipeline run inserts rows into `ops.ExecutionItem` and `ops.ExecutionEvent`.
- Every workflow step transition updates `ops.WorkflowExecution`.
- Querying execution history scans potentially millions of rows.

Keeping this traffic in ConfigurationDb would cause two problems:

1. **Lock contention** — ops writes compete with configuration reads for the same SQL Server transaction log and buffer pool, degrading configuration hot-reload latency.
2. **Growth pressure** — ops history grows without bound; ConfigurationDb is intended to remain small and fast.

OpsDb can be placed on different hardware, given a larger transaction log, or archived independently — none of which is possible when ops shares ConfigurationDb.

## OpsDb Schemas

OpsDb hosts five schemas. All five schemas are owned by `fdw_ops` (full DML).

| Schema | Purpose |
|--------|---------|
| `ops` | Execution tracking (`ops.ExecutionItem`, `ops.ExecutionEvent`, `ops.WorkflowExecution`, `ops.ConnectionHealthCheck`, `ops.ConnectionLimitCounter`) |
| `etl` | ETL runtime data |
| `sched` | Scheduling runtime data |
| `msg` | Messaging runtime data |
| `dq` | Data-quality runtime data |

The schema is deployed via `OpsDb.sqlproj` in the `databases` repository (SQL Server Database Project using Microsoft.Build.Sql SDK). Build the dacpac:

```bash
cd databases/OpsDb
dotnet build -c Release
# Output: bin/Release/OpsDb.dacpac
```

## SQL Login

OpsDb uses a dedicated SQL login `fdw_ops` with WRITE access to all five OpsDb schemas:

```sql
-- databases/OpsDb/security/permissions.sql
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'fdw_ops' AND type = 'S')
BEGIN
    CREATE USER fdw_ops FOR LOGIN fdw_ops;
END
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::ops   TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::etl   TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::sched TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::msg   TO fdw_ops;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dq    TO fdw_ops;
```

The login itself is provisioned at the SQL-Server level; the password is supplied via the `FDW_SECRET_OPS_PASSWORD` environment variable, following the standard `FDW_SECRET_{KEY}` pattern. See [12-10-Secret-Management.md](12-10-Secret-Management.md).

## Registering OpsDb as a Connection

OpsDb must be registered as a named connection in ConfigurationDb (`conn` schema) before application startup. The connection name `"OpsDb"` is the conventional identifier used by all FDW services that write ops data.

Example seed pattern (idempotent — see `databases/ConfigurationDb/seed/02-seed-cfg-runtime-config.sql` for the authoritative seed):

```sql
INSERT INTO conn.[Connection] (Id, Name, ServiceOptionType, Description)
SELECT NEWID(), v.Name, v.ServiceOptionType, v.Description
FROM (VALUES
    ('OpsDb', 'MsSql', 'OpsDb — operational runtime data')
) v(Name, ServiceOptionType, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM conn.[Connection] x
    WHERE x.Name = v.Name AND x.IsCurrent = 1 AND x.IsDeleted = 0
);
```

The typed body (`conn.MsSqlConnection`) provides Server/Database/Port, and the authentication body (`conn.MsSqlConnectionAuthentication`) carries the login + `SecretManagerName`/`SecretKeyName` pointer.

## Application Registration

Register OpsDb-backed execution tracking and escalation services in each server that writes ops data (Api, ETL, Scheduler):

```csharp
builder.Services.AddFrameworkOperations("OpsDb", builder.Configuration, loggerFactory);
// Why: "OpsDb" is the connection name registered in ConfigurationDb. AddFrameworkOperations
// wires IExecutionTracker and IEscalationService to query through IDataGateway against this
// connection at runtime.
```

`AddFrameworkOperations` lives in `Fdw.Hosting/Extensions/ServiceTypeExtensions.cs` and registers `IExecutionTracker`, `IEscalationService`, and the `EscalationConfigurationProvider`.

### appsettings.json

No OpsDb-specific entries are needed in `appsettings.json` — the connection is resolved from ConfigurationDb at runtime, not from static configuration. Escalation policy options bind from the `Operations:EscalationPolicy` section if present.

## Deployment Order

OpsDb must be available before the application starts. Deployment sequence:

1. Deploy ConfigurationDb dacpac (creates every configuration schema — `conn`, `data`, `auth`, `pipe`, `sec`, etc.).
2. Deploy OpsDb dacpac (creates `ops`, `etl`, `sched`, `msg`, `dq`).
3. Deploy AuthDb dacpac (creates the runtime auth schema for credentials and sessions).
4. Run ConfigurationDb seed scripts (registers OpsDb / AuthDb / runtime connections in `conn.Connection` + typed bodies).
5. Start application services (Api, ETL, Scheduler, UI).

## ConfigurationDb Startup

Reference projects configure the ConfigurationDb startup connection in `configurationSchema.json` (loaded into `IConfiguration` at the very start of `Program.cs`). OpsDb does not need a startup entry because it is discovered at runtime via the `"OpsDb"` connection registered in ConfigurationDb.

## Diagnostics

To verify OpsDb connectivity, check structured logs on startup for execution-tracker resolution events emitted by `Fdw.Operations.Execution.ExecutionTrackingService`.

If OpsDb is unreachable, queries through the tracker fail with structured MessageLogging errors and the calling endpoint returns a non-success result.

## Related Documentation

- [Creating a Server](12-01-Creating-A-Server.md) — Startup registration pattern
- [Building an ETL Server](14-02-Building-An-ETL-Server.md) — OpsDb in ETL context
- [Secret Management](12-10-Secret-Management.md) — FDW_SECRET_OPS_PASSWORD
- [Database Schema](08-02-Database-Schema.md) — ConfigurationDb and OpsDb schema overview
- [Connection Configuration Guide](06-06-Connection-Configuration-Guide.md) — Connection registration reference
