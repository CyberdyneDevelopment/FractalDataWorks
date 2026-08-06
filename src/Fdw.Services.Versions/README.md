# Fdw.Services.Versions

Reports the assembly and package versions an FDW application is actually running.

## Types (4)

| Type | Kind | Purpose |
|---|---|---|
| `PackageVersionEnricher` | class | Serilog enricher that adds package version information to log events. Uses assembly metadata (not type… |
| `VersionInfo` | class | Contains version information for a specific assembly or group of assemblies. |
| `VersionLog` | class | MessageLogging for Version Registry operations. EventId range: 7020-7039 |
| `VersionRegistry` | class | Registry for discovering and grouping package versions in the ecosystem. Uses assembly metadata (not… |

## Installation

```bash
dotnet add package Fdw.Services.Versions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
