using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// OTLP exporter - exports telemetry via OpenTelemetry Protocol.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TelemetryExporters), "Otlp", RestrictToCurrentCompilation = true)]
public sealed class OtlpExporter : TelemetryExporterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpExporter"/> class.
    /// </summary>
    public OtlpExporter()
        : base(
            id: 1,
            name: "Otlp",
            description: "Exports telemetry data via OpenTelemetry Protocol (OTLP) to any compatible collector",
            configurationKey: "Otlp",
            supportsTracing: true,
            supportsMetrics: true,
            supportsLogs: true,
            defaultEndpoint: "http://localhost:4317")
    {
    }
}
