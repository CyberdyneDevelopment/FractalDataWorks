# Fdw.Services.Resiliency.RetryNotify

The retry-then-notify strategy — N retries with backoff, then a notification on terminal failure.

This package declares 1 interface(s), 1 configuration type(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `RetryNotifyResiliencyType` | class | RetryNotify resiliency strategy. Retries N times with configurable backoff; on terminal failure,… |

## Contracts (1)

| Type | Kind | Purpose |
|---|---|---|
| `IRetryNotifyResiliencyContext` | interface | Extended execution context for the RetryNotify strategy. Provides access to the notification service for… |

## Configuration (1)

| Type | Kind | Purpose |
|---|---|---|
| `RetryNotifyResiliencyConfiguration` | class | Configuration for the RetryNotify resiliency strategy. Fields map to the settings.RetryNotifyResiliency… |

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `RetryNotifyLog` | class | MessageLogging methods for RetryNotify resiliency strategy. |

## Installation

```bash
dotnet add package Fdw.Services.Resiliency.RetryNotify --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Services.Notifications.Abstractions` · `Fdw.Services.Resiliency`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
