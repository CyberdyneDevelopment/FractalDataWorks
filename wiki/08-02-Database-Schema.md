# Database Schema

FractalDataWorks uses a SQL Server database project (sqlproj) to manage the configuration database schema. This page documents the schema structure and deployment approach.

## Overview

The **ConfigurationDb** database contains 50 tables across 5 schemas, storing all FDW configuration including connections, datasets, authentication, scheduling, and TypeCollection metadata.

## Schema Organization

| Schema | Tables | Purpose |
|--------|--------|---------|
| **cfg** | 34 | Core configuration for service domains |
| **etl** | 5 | ETL pipeline definitions and execution history |
| **sched** | 5 | Job scheduling (cron, interval triggers) |
| **ops** | 3 | Operations (escalation policies, workflow tracking) |
| **types** | 3 | TypeCollection metadata for runtime discovery |

## cfg Schema - Configuration (34 tables)

The cfg schema follows the **ManagedConfiguration** pattern where parent tables store common fields and child tables store type-specific fields.

### Connections

| Table | Description |
|-------|-------------|
| `Connection` | Base table with Id, Name, ServiceOptionType, IsCurrent, IsDeleted |
| `MsSqlConnection` | SQL Server: Server, Database, Encrypt, TrustServerCertificate |
| `HttpConnection` | HTTP/REST: BaseUrl, TimeoutSeconds, DefaultHeaders |
| `OdbcConnection` | ODBC: ConnectionString, DriverName |
| `PostgreSqlConnection` | PostgreSQL: Host, Port, Database, SslMode |

### Secret Managers

| Table | Description |
|-------|-------------|
| `SecretManager` | Base table for secret storage backends |
| `KeyVaultSecretManager` | Azure Key Vault: VaultUri, TenantId, ClientId |

### Authentication

| Table | Description |
|-------|-------------|
| `Authentication` | Base table for auth providers |
| `BasicAuthentication` | Username/password credentials |
| `ApiKeyAuthentication` | API key with header name |
| `OAuth2Authentication` | OAuth2/OIDC: TokenEndpoint, ClientId, Scope |
| `JwtAuthentication` | JWT: Issuer, Audience, SigningKey |

### DataSets

| Table | Description |
|-------|-------------|
| `DataSet` | Base table with Name, DataStoreId, ServiceOptionType |
| `StandardDataSet` | Single-source dataset configuration |
| `FederatedDataSet` | Multi-source with strategy (Union, Merge, etc.) |
| `CompoundDataSet` | Nested/hierarchical dataset structure |
| `DataSetSource` | Maps datasets to their data sources |

### Data Infrastructure

| Table | Description |
|-------|-------------|
| `DataStoreConfiguration` | Physical data store locations |
| `DataPath` | Navigation paths within stores |
| `DataPathSegment` | Individual path segments |
| `DataContainer` | Physical schema definitions |
| `DataContainerField` | Field definitions with type, role, constraints |

### Multi-Tenancy

| Table | Description |
|-------|-------------|
| `Tenants` | Tenant definitions |
| `TenantSettings` | Per-tenant configuration |
| `TenantFeatures` | Feature flags per tenant |
| `TenantRoles` | Roles available in each tenant |
| `Users` | User accounts |
| `UserRoles` | User-role assignments |
| `UserTenants` | User-tenant memberships |

### Other Configuration

| Table | Description |
|-------|-------------|
| `ResiliencyPolicy` | Retry, circuit breaker policies |
| `Notification` | Notification channel configuration |
| `Transformation` | Data transformation definitions |
| `Workflow` | Workflow definitions |
| `ConfigurationAudit` | Audit trail for configuration changes |

## etl Schema - ETL Pipelines (5 tables)

| Table | Description |
|-------|-------------|
| `Pipeline` | Base pipeline with Name, ServiceOptionType, IsEnabled |
| `BatchCopyPipeline` | Batch copy: SourceDataSetId, TargetDataSetId, BatchSize |
| `StreamingPipeline` | Streaming: BufferSize, FlushIntervalSeconds |
| `PipelineExecution` | Execution history with status, timing, row counts |
| `ScheduleExecution` | ETL-specific schedule execution tracking |

## sched Schema - Scheduling (5 tables)

| Table | Description |
|-------|-------------|
| `Schedule` | Base schedule with Name, ServiceOptionType, TimeZoneId |
| `CronSchedule` | Cron expression-based scheduling |
| `IntervalSchedule` | Interval-based (every N seconds) |
| `ScheduleExecution` | Execution history with status, duration |
| `Scheduling` | Scheduler service configuration |

## ops Schema - Operations (3 tables)

| Table | Description |
|-------|-------------|
| `EscalationPolicy` | Alert escalation policy definitions |
| `EscalationLevel` | Levels within each policy (delay, recipients) |
| `WorkflowExecution` | Workflow instance tracking with step progress |

## types Schema - TypeCollection Metadata (3 tables)

The types schema enables runtime TypeCollection discovery from the database.

| Table | Description |
|-------|-------------|
| `TypeCollection` | Collection definitions (FilterOperators, ConnectionTypes, etc.) |
| `TypeOption` | Individual options within collections |
| `TypeProperty` | Properties of each TypeOption for reflection |

## SQL Server Database Project

The schema is managed as a **SQL Server Database Project** using Microsoft.Build.Sql SDK 2.0.0.

### Project Location

```
public/databases/
├── ControlDb.sqlproj          # Project file
├── cfg/
│   ├── cfg.sql                # CREATE SCHEMA cfg
│   └── Tables/                # 34 .sql files
├── etl/
│   ├── etl.sql
│   └── Tables/
├── sched/
│   ├── sched.sql
│   └── Tables/
├── ops/
│   ├── ops.sql
│   └── Tables/
└── types/
    ├── types.sql
    └── Tables/
```

### Building the Dacpac

```powershell
cd public/databases
dotnet build -c Release
# Output: bin/Release/ControlDb.dacpac
```

### Table File Format

Each table file follows this structure:

```sql
CREATE TABLE [schema].[TableName]
(
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    -- columns...
    [IsCurrent] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_TableName] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_...] FOREIGN KEY (...) REFERENCES ...
);
GO

CREATE INDEX [IX_TableName_Column] ON [schema].[TableName]([Column]);
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'Description of the table',
    @level0type = N'SCHEMA', @level0name = N'schema',
    @level1type = N'TABLE', @level1name = N'TableName';
GO
```

### Key Conventions

1. **GO separators** - Required between each statement
2. **Extended properties** - MS_Description on tables and columns
3. **Standard columns** - IsCurrent, IsDeleted for soft-delete pattern
4. **Parent-child FKs** - Child tables reference parent via shared PK
5. **Naming** - PK_Table, FK_Child_Parent, IX_Table_Column, UQ_Table_Column

## Docker Deployment

The Reference Solution deploys the schema via dacpac in Docker:

```powershell
cd samples/ReferenceSolutions/ApiSolution

# Build dacpac
./docker/mssql/build-dacpac.ps1

# Deploy to Docker
docker-compose -f docker-compose.dacpac.yml up -d
```

The Docker image includes SqlPackage for dacpac deployment, with seed data applied afterward.

## Related Documentation

- [ManagedConfiguration](03-01-ManagedConfiguration.md) - How configuration classes map to tables
- [Schema Abstractions](08-01-Schema-Abstractions.md) - Unified schema definitions
- [Reference Solutions Architecture](10-01-ReferenceSolutions-Architecture.md) - Full solution overview
