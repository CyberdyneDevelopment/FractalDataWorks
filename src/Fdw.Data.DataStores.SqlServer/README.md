# Fdw.Data.DataStores.SqlServer

SQL Server data-store support: schema discovery and the containers it exposes.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SqlServerDataStoreResultCodes` | class | TypeCollection for SqlServer DataStore result codes. Codes use the categorized-number catalog scheme (Id… |

## Options (15 declared)

| Type | Kind | Purpose |
|---|---|---|
| `ConnectionIdEmptyCode` | class | ConnectionId cannot be empty. |
| `ConnectionStringEmptyCode` | class | Connection string cannot be null or empty. |
| `DataStoreNullCode` | class | DataStore cannot be null. |
| `DataStoreSaveFailedCode` | class | Failed to save DataStore. |
| `DiscoverTablesFailedCode` | class | Failed to discover tables. |
| `DiscoveryQueryFailedCode` | class | Discovery query failed. |
| `ExistingDataStoreIdEmptyCode` | class | ExistingDataStoreId cannot be empty. |
| `ExtendedPropertiesUnavailableCode` | class | Extended properties query failed (non-fatal). |
| `GetColumnsFailedCode` | class | Failed to get columns. |
| `GetParametersFailedCode` | class | Failed to get parameters. |
| `InvalidConnectionStringCode` | class | Invalid connection string. |
| `SqlServerSchemaImporter` | class | Imports schema from SQL Server databases by querying INFORMATION_SCHEMA views. Returns a discovered with… |
| `StoredProceduresSkippedCode` | class | Stored procedures skipped per options. |
| `WriterCreationFailedCode` | class | Failed to create configuration writer. |
| `WritersCreationFailedCode` | class | Failed to create configuration writers. |

## Installation

```bash
dotnet add package Fdw.Data.DataStores.SqlServer --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Data.Builders` · `Fdw.Data.Importers.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Services.Connections` · `Fdw.Services.Connections.MsSql` · `Fdw.Services.Data` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
