# Fdw.Data.Sqlite

SQLite data support: dialect, translators and native type mapping.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `SqliteDataCommandTranslators` | class | TypeCollection of SQLite data command translators. Discovered at compile-time via TypeCollection source… |
| `SqliteDataResultCodes` | class | TypeCollection for SQLite data command translator result codes. |

## Options (26 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AuthenticationValidationFailedCode` | class | Authentication validation failed for a SQLite connection. |
| `BatchInsertTranslationFailedCode` | class | An unexpected exception during SQLite batch insert translation. |
| `CompoundQueryTranslationFailedCode` | class | An unexpected exception during SQLite compound query translation. |
| `ContainerNotDataContainerCode` | class | Container passed to a SQLite translator does not implement IDataContainer. SQLite query translation… |
| `ContainerNullCode` | class | Container parameter is null. |
| `DeleteTranslationFailedCode` | class | An unexpected exception during SQLite delete translation. |
| `ExecutionFailedCode` | class | SQLite query execution failed. |
| `FindTranslationFailedCode` | class | An unexpected exception during SQLite find translation. |
| `InsertTranslationFailedCode` | class | An unexpected exception during SQLite insert translation. |
| `InvalidCommandTypeCode` | class | Command does not implement the required interface for this translator. |
| `InvalidContainerPathCode` | class | Container path is not an IDatabasePath. |
| `InvalidDataTypeCode` | class | Command data is not an IEnumerable. |
| `MissingDeleteFilterCode` | class | DELETE was attempted without a WHERE filter. |
| `MissingInputDataCode` | class | Command has no input data. |
| `MissingJoinsCode` | class | CompoundQuery requires at least one JOIN expression. |
| `NoFieldsToProjectCode` | class | No columns are available to project — neither a projection expression, schema fields, nor container… |
| `NullPrimaryKeyValueCode` | class | Primary key value is null — cannot build WHERE clause for UPDATE. |
| `QueryTranslationFailedCode` | class | An unexpected exception during SQLite query translation. |

## Installation

```bash
dotnet add package Fdw.Data.Sqlite --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Commands.Data` · `Fdw.Commands.Data.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.Sql`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
