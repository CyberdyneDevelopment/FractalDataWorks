# FractalDataWorks Database Schema

This folder contains the SQL Server Database Project for the ConfigurationDb schema.

## Overview

The **ControlDb.sqlproj** project defines 50 tables across 5 schemas that store FDW configuration:

| Schema | Tables | Purpose |
|--------|--------|---------|
| **cfg** | 34 | Configuration (connections, datasets, auth, tenants) |
| **etl** | 5 | ETL pipelines and execution |
| **sched** | 5 | Scheduling (cron, interval) |
| **ops** | 3 | Operations (escalation, workflows) |
| **types** | 3 | TypeCollection metadata |

## Building

```powershell
# Build the dacpac
dotnet build -c Release

# Output: bin/Release/ControlDb.dacpac (22KB)
```

## Project Structure

```
databases/
├── ControlDb.sqlproj          # SDK 2.0.0 project file
├── cfg/
│   ├── cfg.sql                # CREATE SCHEMA cfg
│   └── Tables/
│       ├── Connection.sql     # Base connection config
│       ├── MsSqlConnection.sql # SQL Server (child of Connection)
│       ├── HttpConnection.sql
│       ├── DataSet.sql
│       ├── Authentication.sql
│       └── ... (34 total)
├── etl/
│   ├── etl.sql
│   └── Tables/
│       ├── Pipeline.sql
│       ├── PipelineExecution.sql
│       └── ... (5 total)
├── sched/
│   ├── sched.sql
│   └── Tables/
│       ├── Schedule.sql
│       ├── CronSchedule.sql
│       └── ... (5 total)
├── ops/
│   ├── ops.sql
│   └── Tables/
│       ├── WorkflowExecution.sql
│       └── ... (3 total)
└── types/
    ├── types.sql
    └── Tables/
        ├── TypeCollection.sql
        ├── TypeOption.sql
        └── TypeProperty.sql
```

## Table Conventions

### Standard Columns

All tables include:
- `Id` - UNIQUEIDENTIFIER primary key (DEFAULT NEWID())
- `IsCurrent` - BIT for soft delete (DEFAULT 1)
- `IsDeleted` - BIT for soft delete (DEFAULT 0)

### Parent-Child Pattern

Child tables share primary key with parent:

```sql
-- Parent table
CREATE TABLE [cfg].[Connection]
(
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    -- common fields...
    CONSTRAINT [PK_Connection] PRIMARY KEY ([Id])
);

-- Child table (shares PK with parent)
CREATE TABLE [cfg].[MsSqlConnection]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [Server] NVARCHAR(200) NOT NULL,
    -- type-specific fields...
    CONSTRAINT [PK_MsSqlConnection] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MsSqlConnection_Connection] FOREIGN KEY ([Id])
        REFERENCES [cfg].[Connection] ([Id])
);
```

### Extended Properties

All tables have `MS_Description` extended properties:

```sql
EXEC sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'SQL Server connection configuration',
    @level0type = N'SCHEMA', @level0name = N'cfg',
    @level1type = N'TABLE', @level1name = N'MsSqlConnection';
GO
```

### GO Separators

**Required** between each statement (SDK requirement):

```sql
CREATE TABLE [...];
GO

CREATE INDEX [...];
GO

EXEC sp_addextendedproperty [...];
GO
```

## Deployment

### Reference Solution (Docker)

```powershell
cd samples/ReferenceSolutions/ApiSolution

# Build dacpac
./docker/mssql/build-dacpac.ps1

# Deploy via Docker with SqlPackage
docker-compose -f docker-compose.dacpac.yml up -d
```

### Manual Deployment

```powershell
# Using SqlPackage
sqlpackage /Action:Publish `
    /SourceFile:bin/Release/ControlDb.dacpac `
    /TargetServerName:localhost `
    /TargetDatabaseName:ConfigurationDb `
    /TargetUser:sa `
    /TargetPassword:YourPassword
```

## SDK Information

Uses Microsoft.Build.Sql SDK 2.0.0:

```xml
<Project DefaultTargets="Build">
  <Sdk Name="Microsoft.Build.Sql" Version="2.0.0" />
  <PropertyGroup>
    <Name>ControlDb</Name>
    <DSP>Microsoft.Data.Tools.Schema.Sql.Sql160DatabaseSchemaProvider</DSP>
  </PropertyGroup>
</Project>
```

## Related Documentation

- [Database Schema Wiki](../wiki/08-02-Database-Schema.md) - Full table reference
- [ManagedConfiguration](../wiki/03-01-ManagedConfiguration.md) - How config classes map to tables
- [Docker README](../samples/ReferenceSolutions/ApiSolution/docker/mssql/README.md) - Deployment guide
