# Fdw.Data.PostgreSql

PostgreSQL data support: dialect, translators, native types and error handlers — the components a PostgreSQL connection composes.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `PostgreSqlConverters` | class | TypeCollection for PostgreSQL data type converters. Child collection of DataTypeConverters. Provides… |
| `PostgreSqlDataCommandTranslators` | class | TypeCollection of PostgreSQL data command translators. Discovered at compile-time via TypeCollection… |
| `PostgreSqlDataResultCodes` | class | TypeCollection for PostgreSQL Data command translator result codes. EventId range: 5500-5599 (within… |
| `PostgreSqlNativeTypes` | class | TypeCollection of all PostgreSQL native data types, each mapping to an abstract for portable DataSet… |

## Options (74 declared)

| Type | Kind | Purpose |
|---|---|---|
| `BatchInsertTranslationFailedCode` | class | Batch insert translation failed with exception. |
| `BoolType` | class | PostgreSQL native type bool — maps to abstract type . |
| `BulkInsertTranslationFailedCode` | class | Bulk insert (COPY) translation failed with exception. |
| `ByteaType` | class | PostgreSQL native type bytea — maps to abstract type . |
| `CharType` | class | PostgreSQL native type char — maps to abstract type . |
| `CompoundQueryTranslationFailedCode` | class | Compound query translation failed with exception. |
| `ContainerNullCode` | class | Container parameter is null. |
| `DateType` | class | PostgreSQL native type date — maps to abstract type . |
| `DeleteTranslationFailedCode` | class | Delete translation failed with exception. |
| `FindTranslationFailedCode` | class | Find translation failed with exception. |
| `Float4Type` | class | PostgreSQL native type float4 — maps to abstract type . |
| `Float8Type` | class | PostgreSQL native type float8 — maps to abstract type . |
| `InsertTranslationFailedCode` | class | Insert translation failed with exception. |
| `Int2Type` | class | PostgreSQL native type int2 — maps to abstract type . |
| `Int4Type` | class | PostgreSQL native type int4 — maps to abstract type . |
| `Int8Type` | class | PostgreSQL native type int8 — maps to abstract type . |
| `InvalidContainerPathCode` | class | Container path is not a PostgreSQL database path. |
| `InvalidDataTypeCode` | class | Input data is not the expected type. |

## Installation

```bash
dotnet add package Fdw.Data.PostgreSql --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Schema.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Sql`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
