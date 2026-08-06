# Fdw.Services.SecretManagers.EnvironmentVariable

The environment-variable secret store — the one an application uses to reach its configuration database before any other store is available.

This package declares 1 configuration type(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `EnvironmentVariableConfigurationCommand` | class | — |

## Configuration (1)

| Type | Kind | Purpose |
|---|---|---|
| `EnvironmentVariableConfiguration` | class | Configuration for Environment Variable secret management service. Inherits from… |

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `EnvironmentVariableConfigurationValidator` | class | Validator for . |

## Installation

```bash
dotnet add package Fdw.Services.SecretManagers.EnvironmentVariable --prerelease
```

## Dependencies

`Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Services` · `Fdw.Services.SecretManagers`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
