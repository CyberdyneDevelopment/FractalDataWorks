# Fdw.Data.MsSql

SQL Server data support: the dialect, the per-command translators, the native type mapping and the error handlers. These are the framework components a connection composes; the connection aggregation itself lives outside FDW.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `MsSqlConverters` | class | TypeCollection for Microsoft SQL Server data type converters. Child collection of DataTypeConverters.… |
| `MsSqlDataCommandTranslators` | class | TypeCollection of MS SQL Server (T-SQL) data command translators. Discovered at compile-time via… |
| `MsSqlDataResultCodes` | class | TypeCollection for MsSql Data command translator result codes. EventId range: 5400-5499 (within… |
| `MsSqlNativeTypes` | class | TypeCollection of SQL Server's native data types — the vocabulary a SQL Server connection speaks. |

## Options (84 declared)

| Type | Kind | Purpose |
|---|---|---|
| `BatchInsertTranslationFailedCode` | class | Batch insert translation failed with exception. |
| `BigIntType` | class | SQL Server native type bigint — normalizes to . |
| `BinaryType` | class | SQL Server native type binary — normalizes to . |
| `BitType` | class | SQL Server native type bit — normalizes to . |
| `BulkInsertTranslationFailedCode` | class | Bulk insert translation failed with exception. |
| `CharType` | class | SQL Server native type Char — normalizes to . |
| `CompoundQueryTranslationFailedCode` | class | Compound query translation failed with exception. |
| `ContainerNullCode` | class | Container parameter is null. |
| `DatabasePathType` | class | Path type for SQL Server database paths (Database.Schema.Object format). |
| `DateTime2Type` | class | SQL Server native type datetime2 — normalizes to . |
| `DateTimeOffsetType` | class | SQL Server native type datetimeoffset — normalizes to . |
| `DateType` | class | SQL Server native type date — normalizes to . |
| `DecimalType` | class | SQL Server native type decimal — normalizes to . |
| `DeleteTranslationFailedCode` | class | Delete translation failed with exception. |
| `FindTranslationFailedCode` | class | Find translation failed with exception. |
| `FloatType` | class | SQL Server native type float — normalizes to . |
| `ImageType` | class | SQL Server native type image — normalizes to . |
| `InsertTranslationFailedCode` | class | Insert translation failed with exception. |

## Installation

```bash
dotnet add package Fdw.Data.MsSql --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Configuration.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Schema.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Sql`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
