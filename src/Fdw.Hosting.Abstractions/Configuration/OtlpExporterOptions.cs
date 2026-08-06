using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// OTLP exporter configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class OtlpExporterOptions
{
    /// <summary>
    /// Gets or sets whether the OTLP exporter is enabled. Default is true when configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the OTLP endpoint URL.
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: OTEL_EXPORTER_OTLP_ENDPOINT
    /// </remarks>
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the export protocol: "Grpc" or "HttpProtobuf". Default is "Grpc".
    /// </summary>
    public string Protocol { get; set; } = "Grpc";

    /// <summary>
    /// Gets or sets optional headers for authentication.
    /// </summary>
    /// <remarks>
    /// Can be set via environment variable: OTEL_EXPORTER_OTLP_HEADERS
    /// </remarks>
    public string? Headers { get; set; }
}
