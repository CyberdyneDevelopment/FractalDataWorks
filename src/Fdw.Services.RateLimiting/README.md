# Fdw.Services.RateLimiting

Rate limiting for FDW services.

This package declares 1 service/provider type(s), 1 model(s).

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `RateLimitRejectionHandler` | class | Handler for rate limit rejections that returns HTTP 429 responses with Retry-After headers. |

## Records (1)

| Type | Kind | Purpose |
|---|---|---|
| `RateLimitRejectionResponse` | class | Response body for rate limit rejection responses. |

## Types (2)

| Type | Kind | Purpose |
|---|---|---|
| `RateLimitLog` | class | MessageLogging methods for rate limiting operations. Every log message is returned in the result AND… |
| `RateLimitingServiceExtensions` | class | Extension methods for registering rate limiting services with dependency injection. |

## Installation

```bash
dotnet add package Fdw.Services.RateLimiting --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.RateLimiting.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
