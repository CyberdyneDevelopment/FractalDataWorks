using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// Base class for telemetry exporter TypeOptions.
/// </summary>
// Why: pure data holder — constructors only assign properties, no branching logic; every
// concrete TypeOption in this hierarchy (ConsoleExporter, OtlpExporter, etc.) is already excluded.
[ExcludeFromCodeCoverage]
public abstract class TelemetryExporterBase : TypeOptionBase<int, TelemetryExporterBase>, ITelemetryExporter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryExporterBase"/> class for Empty sentinel.
    /// </summary>
    protected TelemetryExporterBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
    {
        ExporterConfigurationKey = string.Empty;
        SupportsTracing = false;
        SupportsMetrics = false;
        SupportsLogs = false;
        DefaultEndpoint = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryExporterBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The exporter name.</param>
    /// <param name="description">Description of this exporter.</param>
    /// <param name="configurationKey">The configuration section key.</param>
    /// <param name="supportsTracing">Whether tracing is supported.</param>
    /// <param name="supportsMetrics">Whether metrics are supported.</param>
    /// <param name="supportsLogs">Whether logs are supported.</param>
    /// <param name="defaultEndpoint">The default endpoint URL.</param>
    protected TelemetryExporterBase(
        int id,
        string name,
        string description,
        string configurationKey,
        bool supportsTracing,
        bool supportsMetrics,
        bool supportsLogs,
        string? defaultEndpoint)
        : base(id, name, $"TelemetryExporters:{name}", name, description, "Telemetry")
    {
        ExporterConfigurationKey = configurationKey;
        SupportsTracing = supportsTracing;
        SupportsMetrics = supportsMetrics;
        SupportsLogs = supportsLogs;
        DefaultEndpoint = defaultEndpoint;
    }

    /// <inheritdoc/>
    public string ExporterConfigurationKey { get; }

    /// <inheritdoc/>
    public bool SupportsTracing { get; }

    /// <inheritdoc/>
    public bool SupportsMetrics { get; }

    /// <inheritdoc/>
    public bool SupportsLogs { get; }

    /// <inheritdoc/>
    public string? DefaultEndpoint { get; }
}
