# Token Issuance

This guide describes how an FDW host mints, signs, validates and revokes its own bearer tokens.

> **Replaces the OpenIddict token manager.** Earlier revisions of this page described an `ITokenManager`
> provider axis, a `[ServiceTypeCollection]` named `TokenManagerTypes`, an `OpenIddictTokenManagerType` option,
> grant routing on `/connect/token`, and a signing key loaded by `OpenIddictSigningKeyConfigurator`. FDW-672
> replaced all of it with the step pipeline. There is no `/connect/token`, no `client_credentials` grant, no
> `auth.OpenIddictTokenManager` row and no `OPENIDDICT_SIGNING_KEY` secret. If you are looking for any of them,
> they no longer exist.

## Overview

Signing in and minting are separate. A sign-in flow (the step pipeline; who may use each method is in
[Choosing Who May Use Each Sign-In Method](12-19-Choosing-Who-May-Sign-In.md)) settles who the caller is and what the token may
assert. `AuthenticationRunner` (`Fdw.Services.Authentication/Flow/AuthenticationRunner.cs`) then builds an
`IssuanceRequest` from the completed flow and hands it to `ITokenIssuer`. Neither the pipeline nor the issuer
references the other; the runner is the one place that knows both.

| Piece | Type | Package |
|-------|------|---------|
| Minting seam | `ITokenIssuer` | `Fdw.Services.TokenManagers.Abstractions` |
| Minting implementation | `JwtTokenIssuer` (RFC 9068 access-token profile) | `Fdw.Services.TokenManagers` |
| Signing key seam | `ISigningCredentialProvider` | `Fdw.Services.TokenManagers.Abstractions` |
| Signing key from a secret manager | `SecretManagerSigningCredentialProvider` | `Fdw.Services.TokenManagers` |
| Reads the rows, builds both, once | `JwtIssuanceResolver` (internal; constructs `JwtTokenIssuer` and `SecretManagerSigningCredentialProvider`) | `Fdw.Services.TokenManagers` |
| What the container hands out | `ConfiguredTokenIssuer` / `ConfiguredSigningCredentialProvider` (internal pass-throughs to the resolver) | `Fdw.Services.TokenManagers` |
| Revocation | `ITokenRevocationStore` / `TokenRevocationStore` | `Fdw.Services.TokenManagers(.Abstractions)` |
| Local validation | `LocalKeyAuthenticationHandler` | `Fdw.Services.Authentication` |
| Registration | `TokenManagerTypes` (`[PlatformServiceProvider(ServiceCategory = "TokenManager")]`) | `Fdw.Services.TokenManagers` |

Issuance is exclusive, not pluggable: a deployment mints as exactly one issuer (see the remarks on
`ITokenIssuer`). Validation is the interchangeable half: a host checks its own tokens against its own key, and
accepts another authority's tokens through the flows that verify foreign tokens (the OIDC redirect/callback pair,
`TokenExchangeEndpoint`).

## Registration: `TokenManagerTypes`

`TokenManagerTypes` is a hand-written three-phase registrant, not a `[ServiceTypeCollection]`. FDW-672 removed the
per-option selection, and no `[TypeOption]` was ever declared against the old collection. Referencing the package
enlists it with PlatformServices; no host registers anything itself.

- **Register** adds the `auth.TokenManager` and `auth.JwtTokenManagerConfiguration` configuration providers,
  `JwtIssuanceResolver`, `ITokenIssuer` as `ConfiguredTokenIssuer` and `ISigningCredentialProvider` as
  `ConfiguredSigningCredentialProvider` (pass-throughs, so the container can hand them out before the rows can be
  read; each asks the resolver on use), and the scoped `ITokenRevocationStore`.
- **Initialize** hands the Jwt configuration provider to the token-manager domain, so a row naming
  `Implementation = 'Jwt'` can be composed. Without that step every login failed at issuance with
  "cannot compose 'ApiTokenIssuer'".
- `SigningCredentialCacheLifetime` (10 minutes as declared) is how long a fetched signing key is reused before it
  is read from its secret manager again. Set it at registration to change it.

## Resolution on first use

The rows that say what a host signs as live in a database reached through a gateway that does not exist during
registration. So `JwtIssuanceResolver` reads them the first time anything mints or validates, and builds the
minting and validating halves together from that one read, so that they cannot drift apart on key or issuer.

- It reads the enabled `auth.TokenManager` row whose `Implementation` is `Jwt`. Zero such rows fail every login
  with `No enabled auth.TokenManager row declares Implementation 'Jwt'`.
- `SecretManagerSigningCredentialProvider` fetches the key named by `SecretKeyName` from the secret manager named
  by `SecretManagerName`, imports it as a PEM RSA private key (`ImportFromPem`, a failure carried as a result), and
  signs RS256 with `KeyId` set to the key name. It caches the key for `SigningCredentialCacheLifetime`, so a
  rotation in the secret manager takes effect without a restart.
- The key never appears in configuration, appsettings, a connection string or the host's environment.

## Configuration tables

The domain follows the [polymorphic configuration pattern](03-07-Polymorphic-Configuration-Pattern.md): an
identity header plus one typed body.

**Header: `auth.TokenManager`** (`TokenManagerConfiguration`)

| Column | Notes |
|--------|-------|
| `Id`, `Name` | Identity; the seeded row is `ApiTokenIssuer` |
| `Domain` | `TokenManager` |
| `Implementation` | `Jwt`, the only implementation |

**Typed body: `auth.JwtTokenManagerConfiguration`** (`JwtTokenManagerConfiguration`, keyed on the header's row)

| Column | Notes |
|--------|-------|
| `Issuer` | The `iss` every token carries. Per environment, seeded from `$(AuthAuthority)`, no default |
| `AccessTokenLifetime` | ISO-8601 duration (`PT15M` as seeded) |
| `SecretManagerName` | The secret manager that holds the signing key (`MsSqlSecrets` as seeded) |
| `SecretKeyName` | The signing key's secret name (`TokenSigningKey` as seeded) |

The rows are seeded by `databases-seed/ConfigurationDb/seed/29-seed-jwt-token-manager.sql`. The key itself is
never seed data: `scripts/populate-dev-secrets.sh` writes `TokenSigningKey` into `sec.Secret`, which `MsSqlSecrets`
resolves.

## The issuer must equal the validating authority

A host validates its own tokens with `LocalKeyAuthenticationHandler`, against the LocalKey authority in its
`server-config/auth`. That authority and the `Issuer` above are compared as opaque strings: a trailing slash on
one side and not the other is a mismatch. Minting does not check the issuer and validation does, so when they
disagree the host silently refuses the tokens it has just issued (FDW-751). Copy the string verbatim between the
two; do not retype it.

## Revocation

Access tokens are stateless JWTs; nothing is stored per token at issue. `ITokenRevocationStore` deny-lists them
in the host's `ConnectionRoles.Auth` connection: one token by its `jti` (container `RevokedAccessToken`), or every
token issued to a user up to a moment (container `UserTokenRevocation`). It is a direct, single-purpose store,
because there is exactly one kind of token to revoke.

## Related Documentation

- [Choosing Who May Use Each Sign-In Method](12-19-Choosing-Who-May-Sign-In.md): which sign-in methods each user may use
- [Authentication Architecture](12-11-JWT-Authentication-Architecture.md): server and client token flow
- [Secret Management](12-10-Secret-Management.md): how the signing key is resolved from its secret manager
- [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md): header plus typed body
