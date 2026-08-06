# Fdw.Data.Configuration

Configuration types for the data domain.

This package declares 8 configuration type(s).

## Configuration (8)

| Type | Kind | Purpose |
|---|---|---|
| `DataContainerConfiguration` | class | Configuration class for data containers (physical schemas) at a DataPath. Generates the table… |
| `DataContainerFieldConfiguration` | class | Configuration class for data container fields (columns/properties). Generates the table… |
| `DataContainerKeyConfiguration` | class | Polymorphic identity row for one named key on a DataContainer (Primary, Foreign, Surrogate, Natural,… |
| `DataContainerKeyFieldConfiguration` | class | Pure relationship row: which key, which field, ordinal in composite key. Maps to… |
| `DataPathConfiguration` | class | Configuration class for data paths within a DataStore. Generates the table data.DataPath as a child of… |
| `DataPathPolicyConfiguration` | class | Configuration class for path authorization policies attached to a . Maps to the data.DataPathPolicy… |
| `DataStoreConfiguration` | class | Base configuration class for all data store types. Generates the parent table data.DataStore which… |
| `FileTypeHandlerOverrideConfiguration` | class | Configuration class for per-DataPath file-type handler overrides. Maps to the… |

## Installation

```bash
dotnet add package Fdw.Data.Configuration --prerelease
```

## Dependencies

`Fdw.Configuration` · `Fdw.Configuration.Abstractions` · `Fdw.Data` · `Fdw.Data.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
