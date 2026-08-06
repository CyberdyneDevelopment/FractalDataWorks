using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Console sink - writes logs to standard output.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Sinks), "Console", RestrictToCurrentCompilation = true)]
public sealed class ConsoleSink : SinkBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleSink"/> class.
    /// </summary>
    public ConsoleSink()
        : base(
            id: 1,
            name: "Console",
            description: "Writes log events to the console/standard output",
            configurationKey: "Console",
            supportsStructuredLogging: true,
            requiresNetwork: false)
    {
    }
}
