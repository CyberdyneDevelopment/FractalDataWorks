# Fdw.Data.DataPaths

Data paths: the middle level of the store tree, between a store and its containers.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `DataPathTemplates` | class | TypeCollection of s. Downstream projects register templates via [TypeOption(typeof(DataPathTemplates),… |

## Options (0 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DataPathTemplates` | class | TypeCollection of s. Downstream projects register templates via [TypeOption(typeof(DataPathTemplates),… |
| `IDataPathTemplate` | interface | A reusable shape for a DataPath: parameterized template, target DataStore type, default authorization… |

## Installation

```bash
dotnet add package Fdw.Data.DataPaths --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
