using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// OpenTelemetry sink - exports logs to an OpenTelemetry collector.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Sinks), "OpenTelemetry", RestrictToCurrentCompilation = true)]
public sealed class OpenTelemetrySink : SinkBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetrySink"/> class.
    /// </summary>
    public OpenTelemetrySink()
        : base(
            id: 4,
            name: "OpenTelemetry",
            description: "Exports log events to an OpenTelemetry collector via OTLP",
            configurationKey: "OpenTelemetry",
            supportsStructuredLogging: true,
            requiresNetwork: true)
    {
    }
}
