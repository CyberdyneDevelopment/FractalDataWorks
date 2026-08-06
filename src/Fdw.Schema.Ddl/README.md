# Fdw.Schema.Ddl

DDL generation from FDW's own configuration model.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `DdlResultCodes` | class | Result codes for DDL generation operations. |
| `DdlCommandTypes` | class | — |
| `DdlForeignKeyActions` | class | — |

## Options (17 declared)

| Type | Kind | Purpose |
|---|---|---|
| `CommandGenerationFailedCode` | class | DDL command generation failed. |
| `NoCommandsGeneratedCode` | class | No DDL commands were generated from the schema. |
| `AlterTableDdlCommandType` | class | — |
| `CascadeDdlForeignKeyAction` | class | — |
| `CreateIndexDdlCommandType` | class | — |
| `CreateSchemaDdlCommandType` | class | — |
| `CreateTableDdlCommandType` | class | — |
| `CreateViewDdlCommandType` | class | — |
| `DropIndexDdlCommandType` | class | — |
| `DropSchemaDdlCommandType` | class | — |
| `DropTableDdlCommandType` | class | — |
| `DropViewDdlCommandType` | class | — |
| `InsertDataDdlCommandType` | class | — |
| `NoActionDdlForeignKeyAction` | class | — |
| `RestrictDdlForeignKeyAction` | class | — |
| `SetDefaultDdlForeignKeyAction` | class | — |
| `SetNullDdlForeignKeyAction` | class | — |

## Installation

```bash
dotnet add package Fdw.Schema.Ddl --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results` · `Fdw.Schema.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
