using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Tracing configuration options.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class TracingOptions
{
    /// <summary>
    /// Gets or sets the sampling ratio (0.0 to 1.0). Default is 1.0 (100% sampling).
    /// In production, consider values like 0.1 for 10% sampling.
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets OTLP exporter options. Null disables OTLP export.
    /// </summary>
    public OtlpExporterOptions? Otlp { get; set; }

    /// <summary>
    /// Gets or sets Jaeger exporter options. Null disables Jaeger export.
    /// </summary>
    public JaegerExporterOptions? Jaeger { get; set; }

    /// <summary>
    /// Gets or sets Zipkin exporter options. Null disables Zipkin export.
    /// </summary>
    public ZipkinExporterOptions? Zipkin { get; set; }

    /// <summary>
    /// Gets or sets whether to enable console exporter for debugging. Default is false.
    /// </summary>
    public bool ConsoleExporter { get; set; }
}
