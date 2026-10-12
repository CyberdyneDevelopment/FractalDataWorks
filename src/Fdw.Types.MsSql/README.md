# Fdw.Types.MsSql

SQL Server native type mapping as a TypeCollection.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `MsSqlTypesResultCodes` | class | TypeCollection for Types MsSql result codes. Codes use the categorized-number scheme: Id == EventId ==… |

## Options (9 declared)

| Type | Kind | Purpose |
|---|---|---|
| `CollectionNotFoundCode` | class | TypeCollection not found. |
| `CollectionSaveFailedCode` | class | Failed to save TypeCollection. |
| `DdlResourceNotFoundCode` | class | DDL resource not found. |
| `InvalidConnectionStringCode` | class | Invalid connection string provided. |
| `OptionNotFoundCode` | class | TypeOption not found. |
| `OptionSaveFailedCode` | class | Failed to save TypeOption. |
| `QueryFailedCode` | class | Query failed. |
| `SaveFailedCode` | class | Save operation failed. |
| `SchemaInitializationFailedCode` | class | Schema initialization failed. |

## Installation

```bash
dotnet add package Fdw.Types.MsSql --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Messages` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
