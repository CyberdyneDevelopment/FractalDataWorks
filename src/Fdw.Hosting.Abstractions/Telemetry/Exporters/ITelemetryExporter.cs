namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// Represents a telemetry exporter type for OpenTelemetry configuration.
/// </summary>
public interface ITelemetryExporter
{
    /// <summary>
    /// Gets the unique identifier for this exporter type.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this exporter type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of this exporter type.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the configuration section key for this exporter in appsettings.json.
    /// </summary>
    string ExporterConfigurationKey { get; }

    /// <summary>
    /// Gets whether this exporter supports trace data.
    /// </summary>
    bool SupportsTracing { get; }

    /// <summary>
    /// Gets whether this exporter supports metrics data.
    /// </summary>
    bool SupportsMetrics { get; }

    /// <summary>
    /// Gets whether this exporter supports logs data.
    /// </summary>
    bool SupportsLogs { get; }

    /// <summary>
    /// Gets the default endpoint for this exporter, if applicable.
    /// </summary>
    string? DefaultEndpoint { get; }
}
