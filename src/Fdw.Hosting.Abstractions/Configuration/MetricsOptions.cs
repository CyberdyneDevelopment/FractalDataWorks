using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Metrics configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class MetricsOptions
{
    /// <summary>
    /// Gets or sets OTLP exporter options for metrics. Null disables OTLP export.
    /// </summary>
    public OtlpExporterOptions? Otlp { get; set; }

    /// <summary>
    /// Gets or sets Prometheus exporter options. Null disables Prometheus endpoint.
    /// </summary>
    public PrometheusExporterOptions? Prometheus { get; set; }

    /// <summary>
    /// Gets or sets whether to enable console exporter for debugging. Default is false.
    /// </summary>
    public bool ConsoleExporter { get; set; }

    /// <summary>
    /// Gets or sets the metrics export interval in seconds. Default is 60.
    /// </summary>
    public int ExportIntervalSeconds { get; set; } = 60;
}
