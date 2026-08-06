# Fdw.Web.Http.Authentication.Blazor

Blazor circuit-aware token handling for authenticated HTTP.

This package declares 2 service/provider type(s).

## Services (2)

| Type | Kind | Purpose |
|---|---|---|
| `BlazorServerAccessTokenProvider` | class | Provides access tokens for Blazor Server applications by reading from the first, falling back to for SSR… |
| `TokenCapturingCircuitHandler` | class | Scoped that captures the access token from HttpContext when the circuit connects (WebSocket handshake --… |

## Types (3)

| Type | Kind | Purpose |
|---|---|---|
| `BlazorAuthLog` | class | MessageLogging for Blazor Server authentication operations. EventId range: 4440-4459 |
| `CircuitTokenAccessor` | class | Singleton that holds the current circuit's access token in an . The sets the value before each inbound… |
| `ServiceCollectionExtensions` | class | Extension methods for registering Blazor Server authentication services. |

## Installation

```bash
dotnet add package Fdw.Web.Http.Authentication.Blazor --prerelease
```

## Dependencies

`Fdw.Web.Http.Authentication`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
