using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// Console exporter - writes telemetry data to console for debugging.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TelemetryExporters), "Console", RestrictToCurrentCompilation = true)]
public sealed class ConsoleExporter : TelemetryExporterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleExporter"/> class.
    /// </summary>
    public ConsoleExporter()
        : base(
            id: 4,
            name: "Console",
            description: "Writes telemetry data to console for debugging and development",
            configurationKey: "Console",
            supportsTracing: true,
            supportsMetrics: true,
            supportsLogs: true,
            defaultEndpoint: null)
    {
    }
}
