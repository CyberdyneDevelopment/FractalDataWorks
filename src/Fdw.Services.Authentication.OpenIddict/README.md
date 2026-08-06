# Fdw.Services.Authentication.OpenIddict

The OpenIddict token manager. Deliberately not split from its service-type surface — its aggregation depends on its own registration, and that is a fact about the design rather than an obstacle.

This package declares 1 configuration type(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `OpenIddictTokenManagerConfigurationCommand` | class | ConfigurationCommands TypeOption for the OpenIddictTokenManager typed-body domain. Routes configuration… |

## Configuration (1)

| Type | Kind | Purpose |
|---|---|---|
| `OpenIddictTokenManagerConfiguration` | class | Typed-body configuration for the OpenIddict token-manager TypeOption. Standalone POCO — does NOT inherit… |

## Installation

```bash
dotnet add package Fdw.Services.Authentication.OpenIddict --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Extensions` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Authentication` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.ExternalIdentityProviders` · `Fdw.Services.ExternalIdentityProviders.Abstractions` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions` · `Fdw.Services.TokenManagers` · `Fdw.Services.TokenManagers.Abstractions` · `Fdw.Services.Users.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
