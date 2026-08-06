using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// Zipkin exporter - exports trace data to Zipkin.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TelemetryExporters), "Zipkin", RestrictToCurrentCompilation = true)]
public sealed class ZipkinExporter : TelemetryExporterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZipkinExporter"/> class.
    /// </summary>
    public ZipkinExporter()
        : base(
            id: 3,
            name: "Zipkin",
            description: "Exports trace data to Zipkin distributed tracing system",
            configurationKey: "Zipkin",
            supportsTracing: true,
            supportsMetrics: false,
            supportsLogs: false,
            defaultEndpoint: "http://localhost:9411/api/v2/spans")
    {
    }
}
