# Fdw.Configuration

Database-backed configuration: `[ManagedConfiguration]` POCOs that generate their own DDL, validation and UI form metadata, read back as rows rather than from JSON files.

This package declares 1 interface(s), 1 service/provider type(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `ISchemaHashService` | interface | Interface for calculating schema hashes for change detection. |

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `FdwConfigurationProvider` | class | Generic multi-tenant configuration provider that flattens hierarchical configuration data. Works with… |

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `ConfigurationWrapper<TSettings>` | class | Wrapper for configuration instances that separates header metadata from domain-specific settings.… |

## Installation

```bash
dotnet add package Fdw.Configuration --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
