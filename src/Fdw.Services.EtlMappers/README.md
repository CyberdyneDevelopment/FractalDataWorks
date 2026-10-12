# Fdw.Services.EtlMappers

Row mappers for ETL: the per-shape mapping options an ETL pipeline resolves by name.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `EtlRowMapperTypes` | class | Collection of ETL row mapper types with Configure/Register/Initialize pattern. |

## Installation

```bash
dotnet add package Fdw.Services.EtlMappers --prerelease
```

## Dependencies

`Fdw.Configuration` · `Fdw.Data.RowSources.Abstractions` · `Fdw.Results` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.EtlMappers.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
