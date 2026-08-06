using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// Prometheus exporter - exposes metrics via HTTP endpoint for Prometheus scraping.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TelemetryExporters), "Prometheus", RestrictToCurrentCompilation = true)]
public sealed class PrometheusExporter : TelemetryExporterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrometheusExporter"/> class.
    /// </summary>
    public PrometheusExporter()
        : base(
            id: 5,
            name: "Prometheus",
            description: "Exposes metrics via HTTP endpoint for Prometheus scraping",
            configurationKey: "Prometheus",
            supportsTracing: false,
            supportsMetrics: true,
            supportsLogs: false,
            defaultEndpoint: "/metrics")
    {
    }
}
