# Fdw.Services.TokenManagers

The token-manager domain: minting, signing and revoking this host's own bearer tokens.

Since FDW-672 this is not a collection of kinds. `TokenManagerTypes` is one hand-written three-phase registrant
(`[PlatformServiceProvider(ServiceCategory = "TokenManager")]`): **Register** adds the `auth.TokenManager` and
`auth.JwtTokenManagerConfiguration` configuration providers, `JwtIssuanceResolver`, `ITokenIssuer` (as the
`ConfiguredTokenIssuer` pass-through), `ISigningCredentialProvider` (as `ConfiguredSigningCredentialProvider`) and the
scoped `ITokenRevocationStore`; **Initialize** connects the Jwt configuration provider to the token-manager domain.
Referencing this package is what enlists it; no host registers anything itself. A deployment mints as exactly one
issuer: the enabled `auth.TokenManager` row whose `Implementation` is `Jwt`.

## Types

| Type | Purpose |
|---|---|
| `TokenManagerTypes` | The registrant above; also declares `SigningCredentialCacheLifetime` (how long a fetched signing key is reused) |
| `JwtIssuanceResolver` | Reads the Jwt token manager rows on first use and builds `JwtTokenIssuer` and `SecretManagerSigningCredentialProvider` from them, together |
| `JwtTokenIssuer` | Mints a signed RS256 JWT (RFC 9068 access-token profile) asserting what a completed sign-in flow established |
| `SecretManagerSigningCredentialProvider` | Fetches the PEM signing key by name from the named secret manager, caches it, re-reads it when the cache lapses |
| `TokenRevocationStore` | Deny-lists a token by `jti` (`RevokedAccessToken`) or every token of a user up to a moment (`UserTokenRevocation`) |
| `JwtTokenManagerConfiguration` | The typed body: `Issuer`, `AccessTokenLifetime`, `SecretManagerName`, `SecretKeyName`, all required |

How it fits together, with the configuration tables and failure messages: `public/wiki/10-03-Building-Authentication-Service.md`.

## Installation

```bash
dotnet add package Fdw.Services.TokenManagers --prerelease
```

## Dependencies

`Fdw.Commands.Data` · `Fdw.Commands.Data.Fluency` · `Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Execution.Abstractions` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.SecretManagers` · `Fdw.Services.SecretManagers.Abstractions` · `Fdw.Services.TokenManagers.Abstractions` · `Fdw.Services.Users` · `Fdw.Services.Users.Abstractions` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04 by a script that is not kept; Dependencies checked against the csproj on 2026-10-25 -->
