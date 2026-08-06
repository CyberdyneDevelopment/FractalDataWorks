using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Jaeger exporter configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class JaegerExporterOptions
{
    /// <summary>
    /// Gets or sets whether the Jaeger exporter is enabled. Default is true when configured.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Jaeger agent host.
    /// </summary>
    public string AgentHost { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the Jaeger agent port.
    /// </summary>
    public int AgentPort { get; set; } = 6831;
}
