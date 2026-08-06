using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Seq sink - writes logs to a Seq server for structured log analysis.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Sinks), "Seq", RestrictToCurrentCompilation = true)]
public sealed class SeqSink : SinkBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeqSink"/> class.
    /// </summary>
    public SeqSink()
        : base(
            id: 3,
            name: "Seq",
            description: "Writes log events to a Seq server for structured log analysis and querying",
            configurationKey: "Seq",
            supportsStructuredLogging: true,
            requiresNetwork: true)
    {
    }
}
