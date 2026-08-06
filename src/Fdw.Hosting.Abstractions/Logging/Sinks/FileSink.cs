using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// File sink - writes logs to a file on disk.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Sinks), "File", RestrictToCurrentCompilation = true)]
public sealed class FileSink : SinkBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSink"/> class.
    /// </summary>
    public FileSink()
        : base(
            id: 2,
            name: "File",
            description: "Writes log events to a file on disk with optional rolling",
            configurationKey: "File",
            supportsStructuredLogging: true,
            requiresNetwork: false)
    {
    }
}
