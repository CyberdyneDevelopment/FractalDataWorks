# Fdw.Services.Authentication

Authentication services and the token-issuing surface behind them.

This package declares 2 model(s).

## Options (19)

| Type | Kind | Purpose |
|---|---|---|
| `AccessToken` | class | OAuth 2.0 Access Token type. |
| `ApiKeyAuthenticationMethod` | class | API Key authentication method. |
| `AuthorizationCodeFlow` | class | OAuth 2.0 Authorization Code flow. |
| `BearerTokenAuthenticationMethod` | class | Bearer token authentication method. |
| `BearerTokenType` | class | Bearer token type for HTTP Authorization header. |
| `CertificateAuthenticationMethod` | class | Certificate-based authentication method. |
| `ClientCredentialsFlow` | class | OAuth 2.0 Client Credentials flow. |
| `DeviceCodeFlow` | class | OAuth 2.0 Device Code flow. |
| `FormBasedAuthenticationMethod` | class | Form-based authentication method. ExtendedEnum that wraps Microsoft's form-based authentication with… |
| `IdToken` | class | OpenID Connect ID Token type. |
| `InteractiveFlow` | class | Interactive authentication flow with user interaction. |
| `JwtAuthenticationMethod` | class | JWT (JSON Web Token) authentication method. |

## Records (2)

| Type | Kind | Purpose |
|---|---|---|
| `BasicAuthenticationRecord` | class | Data record for the auth.BasicAuthentication table. Represents basic (username/password) authentication… |
| `OAuth2AuthenticationRecord` | class | Data record for the auth.OAuth2Authentication table. Represents OAuth2 client credentials and token… |

## Types (3)

| Type | Kind | Purpose |
|---|---|---|
| `AuthenticationLogger` | class | Static logger class for authentication provider operations. |
| `AuthenticationProviderLogger` | class | Static logger class for authentication provider operations. |
| `DefaultPrincipalResolver` | class | Default — resolves the FDW claims principal by combining tenant/org context with… |

## Installation

```bash
dotnet add package Fdw.Services.Authentication --prerelease
```

## Dependencies

`Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Services` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization` · `Fdw.Services.Authorization.Abstractions` · `Fdw.Services.Connections.Abstractions` · `Fdw.Services.Multitenancy.Abstractions` · `Fdw.Services.Users` · `Fdw.Services.Users.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
