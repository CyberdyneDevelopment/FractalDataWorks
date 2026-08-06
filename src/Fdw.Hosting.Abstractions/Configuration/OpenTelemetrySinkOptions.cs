using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// OpenTelemetry sink configuration options for log export.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class OpenTelemetrySinkOptions
{
    /// <summary>
    /// Gets or sets whether OTLP log export is enabled. Default is true when configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the OTLP endpoint URL.
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: FdwHost__Logging__OpenTelemetry__Endpoint
    /// Or via standard OTEL_EXPORTER_OTLP_ENDPOINT
    /// </remarks>
    public string Endpoint { get; set; } = "http://localhost:4317";
}
