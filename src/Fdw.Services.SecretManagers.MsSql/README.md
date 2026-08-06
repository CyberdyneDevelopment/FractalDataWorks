# Fdw.Services.SecretManagers.MsSql

The SQL Server secret store.

This package declares 1 configuration type(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `MsSqlSecretManagerConfigurationCommand` | class | — |

## Configuration (1)

| Type | Kind | Purpose |
|---|---|---|
| `MsSqlSecretManagerConfiguration` | class | Configuration for MsSql secret management service. Inherits from SecretManagerConfiguration for common… |

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `MsSqlSecretManagerConfigurationValidator` | class | Validator for . |

## Installation

```bash
dotnet add package Fdw.Services.SecretManagers.MsSql --prerelease
```

## Dependencies

`Fdw.Commands.Data.Extensions` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Security.Hashing` · `Fdw.Services` · `Fdw.Services.Connections` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Connections.MsSql` · `Fdw.Services.SecretManagers`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
