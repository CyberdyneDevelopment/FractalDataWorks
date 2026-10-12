# Fdw.Calculations.SignalR

The SignalR hub that broadcasts calculation progress and results.

This package declares 2 interface(s).

## Options (1)

| Type | Kind | Purpose |
|---|---|---|
| `CalculationHubOption` | class | Registers the calculation hub against the collection. |

## Contracts (2)

| Type | Kind | Purpose |
|---|---|---|
| `ICalculationHubClient` | interface | Client-side SignalR hub interface for calculation notifications. |
| `ICalculationNotifier` | interface | Service for sending real-time calculation notifications via SignalR. |

## Types (8)

| Type | Kind | Purpose |
|---|---|---|
| `CacheStatisticsEvent` | record | Event raised to broadcast cache statistics. |
| `CalculationCompletedEvent` | record | Event raised when a calculation completes successfully. |
| `CalculationFailedEvent` | record | Event raised when a calculation fails. |
| `CalculationHub` | class | SignalR hub for real-time calculation notifications. |
| `CalculationNotifier` | class | Default implementation of using SignalR. |
| `CalculationProgressEvent` | record | Event raised to report calculation progress. |
| `CalculationResultSummary` | record | Summary of a calculation result. |
| `CalculationStartedEvent` | record | Event raised when a calculation starts. |

## Installation

```bash
dotnet add package Fdw.Calculations.SignalR --prerelease
```

## Dependencies

`Fdw.SignalR`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
