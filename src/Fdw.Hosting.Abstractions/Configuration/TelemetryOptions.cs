using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Telemetry configuration options for OpenTelemetry integration.
/// Binds to the "FdwHost:Telemetry" section in appsettings.json.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class TelemetryOptions
{
    /// <summary>
    /// Gets or sets the service name for telemetry. Defaults to application name.
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: OTEL_SERVICE_NAME or FdwHost__Telemetry__ServiceName
    /// </remarks>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for telemetry. Defaults to assembly version.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the service namespace for telemetry grouping.
    /// </summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Gets or sets whether tracing is enabled. Default is true.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether metrics collection is enabled. Default is true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets tracing-specific options.
    /// </summary>
    public TracingOptions Tracing { get; set; } = new();

    /// <summary>
    /// Gets or sets metrics-specific options.
    /// </summary>
    public MetricsOptions Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets auto-instrumentation options.
    /// </summary>
    public InstrumentationOptions Instrumentation { get; set; } = new();
}
