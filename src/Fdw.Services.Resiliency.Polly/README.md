# Fdw.Services.Resiliency.Polly

The Polly retry strategy — backoff, optional circuit breaker and timeout.

This package declares 1 configuration type(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `PollyRetryResiliencyType` | class | PollyRetry resiliency strategy. Wraps stage execution in a Polly ResiliencePipeline configured from . |

## Configuration (1)

| Type | Kind | Purpose |
|---|---|---|
| `PollyRetryResiliencyConfiguration` | class | Configuration for the PollyRetry resiliency strategy. Fields map to the settings.PollyRetryResiliency… |

## Types (1)

| Type | Kind | Purpose |
|---|---|---|
| `PollyRetryLog` | class | MessageLogging methods for PollyRetry resiliency strategy. |

## Installation

```bash
dotnet add package Fdw.Services.Resiliency.Polly --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Services.Resiliency`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
