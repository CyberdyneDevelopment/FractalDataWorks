# Fdw.Services.Configuration.Abstractions

The configuration-service contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IConfigurationCommand` | interface | Marker interface for configuration commands — commands that target configuration data (per-domain… |
| `IConfigurationCommands` | interface | Non-generic marker interface for configuration command type collections. Implemented by… |

## Installation

```bash
dotnet add package Fdw.Services.Configuration.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Commands.Data.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
