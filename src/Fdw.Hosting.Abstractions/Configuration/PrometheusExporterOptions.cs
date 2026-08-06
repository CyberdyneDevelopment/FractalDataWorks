using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Prometheus exporter configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class PrometheusExporterOptions
{
    /// <summary>
    /// Gets or sets whether the Prometheus exporter is enabled. Default is true when configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the endpoint path for metrics scraping. Default is "/metrics".
    /// </summary>
    public string Endpoint { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets the port for the Prometheus HTTP listener (for non-web hosts).
    /// </summary>
    public int? Port { get; set; }
}
