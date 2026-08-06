using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Zipkin exporter configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class ZipkinExporterOptions
{
    /// <summary>
    /// Gets or sets whether the Zipkin exporter is enabled. Default is true when configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Zipkin endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";
}
