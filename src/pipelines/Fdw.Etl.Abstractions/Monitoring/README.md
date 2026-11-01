# Fdw.Etl.Monitoring.Abstractions

Abstractions for ETL pipeline monitoring including metrics, health checks, and performance tracking.

## Overview

Monitoring contracts for ETL operations:

- **Metrics Collection**: Track pipeline performance
- **Health Checks**: Monitor pipeline health
- **Progress Tracking**: Real-time progress updates
- **Error Tracking**: Capture and report errors
- **Audit Logging**: Comprehensive audit trails

**Target Frameworks**: .NET Standard 2.0, .NET 10.0

## Key Interfaces

### IMonitor

```csharp
public interface IMonitor
{
    Task<IGenericResult> RecordMetricAsync(string name, double value);
    Task<IGenericResult> RecordEventAsync(string eventName, IDictionary<string, object> metadata);
    Task<IGenericResult<HealthStatus>> CheckHealthAsync();
}
```

## Usage

```csharp
await monitor.RecordMetricAsync("rows_processed", 1000);
await monitor.RecordEventAsync("pipeline_started", metadata);
var health = await monitor.CheckHealthAsync();
```

## Summary

Fdw.Etl.Monitoring.Abstractions provides contracts for comprehensive ETL pipeline monitoring and observability.
