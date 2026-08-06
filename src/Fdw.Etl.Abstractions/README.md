# Fdw.Etl.Abstractions

ETL contracts — jobs, schedules and the monitoring surface around a run.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (12)

| Type | Kind | Purpose |
|---|---|---|
| `IAlert` | interface | Represents an alert to be sent. |
| `IAlertRule` | interface | Represents an alert rule. |
| `IAlertSeverity` | interface | Interface for alert severity levels. Extends ITypeOption to enable TypeCollection discovery. |
| `IAlertingService` | interface | Service for sending alerts and notifications. |
| `IComparisonOperator` | interface | Interface for comparison operators used in alert rules. Extends ITypeOption to enable TypeCollection… |
| `IEtlMetricsCollector` | interface | Collects metrics for ETL pipeline executions. |
| `IEtlTelemetryService` | interface | Service for sending telemetry events. |
| `IHealthCheckResult` | interface | Represents the result of a health check. |
| `IHealthCheckService` | interface | Service for performing health checks. |
| `IHealthState` | interface | Interface for health states. Extends ITypeOption to enable TypeCollection discovery. |
| `IHealthStatus` | interface | Represents overall health status. |
| `ISeverityLevel` | interface | Interface for severity levels used in telemetry traces. Extends ITypeOption to enable TypeCollection… |

## Base types (4)

| Type | Kind | Purpose |
|---|---|---|
| `AlertSeverityBase` | class | Base class for alert severity levels. |
| `ComparisonOperatorBase` | class | Base class for comparison operators used in alert rules. |
| `HealthStateBase` | class | Base class for health states. |
| `SeverityLevelBase` | class | Base class for severity levels used in telemetry traces. |

## Models and supporting types (23)

| Type | Kind | Purpose |
|---|---|---|
| `AlertSeverities` | class | TypeCollection for alert severity levels. |
| `ComparisonOperators` | class | TypeCollection for comparison operators used in alert rules. |
| `CriticalSeverity` | class | Critical alert severity. |
| `CriticalSeverityLevel` | class | Critical error message. |
| `DegradedState` | class | Service is degraded but functional. |
| `EqualOperator` | class | Equal to operator. |
| `ErrorSeverity` | class | Error alert severity. |
| `ErrorSeverityLevel` | class | Error message. |
| `GreaterThanOperator` | class | Greater than operator. |
| `GreaterThanOrEqualOperator` | class | Greater than or equal to operator. |
| `HealthStateJsonConverter` | class | JSON converter for . Reads the state name (from either a bare string token or the name property of an… |
| `HealthStates` | class | TypeCollection for health states. |

## Installation

```bash
dotnet add package Fdw.Etl.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Scheduling.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
