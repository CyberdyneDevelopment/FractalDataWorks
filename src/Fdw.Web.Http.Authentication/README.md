# Fdw.Web.Http.Authentication

Authenticated HTTP handlers — token attachment and refresh.

This package declares 5 interface(s), 3 service/provider type(s).

## Contracts (5)

| Type | Kind | Purpose |
|---|---|---|
| `IAccessTokenProvider` | interface | Abstracts how access tokens are obtained for HTTP requests. |
| `IApiKeyProvider` | interface | Abstracts how API keys are obtained for HTTP requests. Implement this interface to provide a static API… |
| `IAuthExpirationNotifier` | interface | Notifies the authentication system when a session has expired (refresh token rejected). Implementations… |
| `ITokenRefreshCoordinator` | interface | Coordinates token refresh operations to prevent concurrent refresh calls. When multiple callers request… |
| `ITokenRefreshHandler` | interface | Handles token refresh operations for expired or expiring tokens. |

## Services (3)

| Type | Kind | Purpose |
|---|---|---|
| `ApiKeyDelegatingHandler` | class | A delegating handler that attaches an API key header to outgoing HTTP requests. Use this instead of when… |
| `BearerTokenHandler` | class | A delegating handler that attaches a bearer token to outgoing HTTP requests. |
| `RetryingBearerTokenHandler` | class | A delegating handler that attaches a bearer token to outgoing HTTP requests and retries once with a… |

## Types (4)

| Type | Kind | Purpose |
|---|---|---|
| `ApiKeyLog` | class | MessageLogging for API key HTTP handler operations. EventId range: 4430-4435 |
| `BearerTokenLog` | class | MessageLogging for bearer token HTTP handler operations. EventId range: 4420-4425 |
| `DefaultTokenRefreshCoordinator` | class | Default coordinator that serializes refresh operations via and skips redundant refreshes within a… |
| `ServiceCollectionExtensions` | class | Extension methods for registering bearer token authentication handlers. |

## Installation

```bash
dotnet add package Fdw.Web.Http.Authentication --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Services.Authentication.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
