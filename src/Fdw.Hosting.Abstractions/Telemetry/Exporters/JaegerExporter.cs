using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// Jaeger exporter - exports trace data to Jaeger.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TelemetryExporters), "Jaeger", RestrictToCurrentCompilation = true)]
public sealed class JaegerExporter : TelemetryExporterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JaegerExporter"/> class.
    /// </summary>
    public JaegerExporter()
        : base(
            id: 2,
            name: "Jaeger",
            description: "Exports trace data to Jaeger distributed tracing system",
            configurationKey: "Jaeger",
            supportsTracing: true,
            supportsMetrics: false,
            supportsLogs: false,
            defaultEndpoint: "http://localhost:14268/api/traces")
    {
    }
}
