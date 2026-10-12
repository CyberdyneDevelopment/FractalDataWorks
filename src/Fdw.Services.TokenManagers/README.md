# Fdw.Services.TokenManagers

The token-manager domain — issuing and validating tokens, as a collection of kinds.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `TokenManagerTypes` | class | Collection of token manager service types. Structurally copies SchedulerTypes and is collected by… |

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `TokenManagerConfiguration` | class | Header configuration for token manager services representing the auth.TokenManager parent table. Fields… |
| `TokenManagerTypeBase<TService, TConfiguration, TFactory>` | class | Base class for token manager service type definitions. Structurally copies SchedulerTypeBase… |

## Installation

```bash
dotnet add package Fdw.Services.TokenManagers --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Execution.Abstractions` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions` · `Fdw.Services.TokenManagers.Abstractions` · `Fdw.Services.Users` · `Fdw.Services.Users.Abstractions` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
