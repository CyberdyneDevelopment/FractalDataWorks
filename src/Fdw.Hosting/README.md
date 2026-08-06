# Fdw.Hosting

Host construction for an FDW application: the startup order, the phase invocation, and the middleware a host installs.

This package declares 1 interface(s), 1 model(s).

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IWebMcpToolRegistry` | interface | Provides access to the set of WebMCP tools discovered from decorated endpoints. |

## Records (1)

| Type | Kind | Purpose |
|---|---|---|
| `ErrorResponse` | class | Standard error response for 500 Internal Server Error responses. Includes request ID for support… |

## Types (32)

| Type | Kind | Purpose |
|---|---|---|
| `CorsOptions` | class | Configuration options for CORS policy. Loaded from appsettings.json "Cors" section. |
| `ErrorEnvelopeExtensions` | class | Helpers for emitting consistent {errorCode, messages} JSON envelopes from any endpoint. |
| `GlobalExceptionHandlerMiddleware` | class | Global exception handler middleware that catches unhandled exceptions and returns a standardized error… |
| `HostingLog` | class | MessageLogging for FDW hosting operations. EventId range: 500-550 |
| `MiddlewarePipelineExtensions` | class | Extension methods for configuring the full HTTP middleware pipeline in correct order. |
| `RequestContextLog` | class | Source-generated logging methods for RequestContextMiddleware. EventId range: 549 |
| `RequestContextMiddleware` | class | Middleware that builds an from the authenticated and stores it in for downstream use. Also establishes… |
| `ResponseBufferingExtensions` | class | Extension methods for the response-buffering middleware. |
| `ResponseBufferingMiddleware` | class | Buffers response bodies into a MemoryStream so Content-Length is set explicitly, preventing chunked… |
| `ResponseBufferingOptions` | class | Configuration for the framework response-buffering middleware. |
| `SecurityHeadersOptions` | class | Configuration options for security headers middleware. |
| `ServiceEndpointsOptions` | class | Downstream service endpoint URLs for API gateway proxying. Loaded from appsettings.json… |

## Installation

```bash
dotnet add package Fdw.Hosting --prerelease
```

## Dependencies

`Fdw.Hosting.Abstractions` · `Fdw.Operations` · `Fdw.Services.Agents` · `Fdw.Services.Audit` · `Fdw.Services.Authentication` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Services.Authorization` · `Fdw.Services.Calculations` · `Fdw.Services.Connections` · `Fdw.Services.Data` · `Fdw.Services.Etl` · `Fdw.Services.Etl.Projects` · `Fdw.Services.Messaging` · `Fdw.Services.Multitenancy.Sql` · `Fdw.Services.Notifications` · `Fdw.Services.Pipelines` · `Fdw.Services.Quality` · `Fdw.Services.RateLimiting` · `Fdw.Services.Resiliency` · `Fdw.Services.Scheduling` · `Fdw.Services.SecretManagers` · `Fdw.Services.SessionState` · `Fdw.Services.Settings` · `Fdw.Services.Users` · `Fdw.Services.Workflows` · `Fdw.SignalR` · `Fdw.Web.RestEndpoints`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
