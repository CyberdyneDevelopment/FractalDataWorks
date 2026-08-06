# Fdw.Services.TokenManagers.Abstractions

The token-manager contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (7)

| Type | Kind | Purpose |
|---|---|---|
| `IAuthenticationService` | interface | The generic, provider-agnostic authN service — the single "front door" a host injects for… |
| `ITokenManager` | interface | Provider axis for token issuance/validation/invalidation/claims-extraction. One implementation backs one… |
| `ITokenManagerConfiguration` | interface | Marker interface for typed token-manager body configurations (e.g. an OpenIddict-specific configuration… |
| `ITokenManagerFactory` | interface | Marker interface for token manager factories. Mirrors ISchedulingFactory: the non-generic marker lets… |
| `ITokenManagerFactory<TService, TConfiguration>` | interface | Generic interface for token manager factories with typed configuration. |
| `ITokenManagerType` | interface | Non-generic interface for token manager service types. |
| `ITokenManagerType<TService, TConfiguration, TFactory>` | interface | Interface for token manager service types. Mirrors ISchedulerType's generic/non-generic split, but stays… |

## Models and supporting types (2)

| Type | Kind | Purpose |
|---|---|---|
| `TokenIssuanceRequest` | class | Carries the grant information for a token issuance request. FDW always mints its OWN role/permission… |
| `TokenIssuanceResult` | class | The result of a successful token issuance or refresh operation. Access and refresh tokens are returned… |

## Installation

```bash
dotnet add package Fdw.Services.TokenManagers.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Configuration.Abstractions` · `Fdw.Messages` · `Fdw.Orchestration.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.Services.Execution.Abstractions` · `Fdw.Services.SecretManagers.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
