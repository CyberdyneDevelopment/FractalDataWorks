# Fdw.Data.RowSources

Row sources — the streaming read surface that feeds ETL and bulk operations.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `RecordSourceTypes` | class | TypeCollection for record source types (DataReader, Xml, Json, Delimited, FixedWidth, Http). The… |
| `RecordWriterTypes` | class | TypeCollection for record writer types (Json, Xml, Delimited, FixedWidth). The write-side mirror of :… |
| `RowMapperTypes` | class | TypeCollection for row mapper types (Pooled, Dynamic). |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `PooledRowMapperType` | class | TypeOption for pooled row mappers. |

## Installation

```bash
dotnet add package Fdw.Data.RowSources --prerelease
```

## Dependencies

`Fdw.Data.RowSources.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
