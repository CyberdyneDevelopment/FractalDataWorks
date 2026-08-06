# Fdw.Aui

The Agent-User-Interface surface — the manifest a host publishes so an agent can drive it.

This package declares 1 service/provider type(s).

## Services (1)

| Type | Kind | Purpose |
|---|---|---|
| `AuiService` | class | Service for aggregating and managing Agent User Interface (AUI) metadata. |

## Types (2)

| Type | Kind | Purpose |
|---|---|---|
| `AuiLog` | class | MessageLogging for Agent User Interface (AUI) operations. EventId range: 7100-7199 |
| `AuiMiddleware` | class | Middleware that intercepts agent requests and serves the AUI manifest. |

## Installation

```bash
dotnet add package Fdw.Aui --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Aui.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
