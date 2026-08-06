using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Verbose log level - most detailed logging, typically only for development.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LogLevels), "Verbose", RestrictToCurrentCompilation = true)]
public sealed class VerboseLogLevel : LogLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerboseLogLevel"/> class.
    /// </summary>
    public VerboseLogLevel()
        : base(
            id: 0,
            name: "Verbose",
            description: "Most detailed logging level, typically only enabled during development",
            serilogLevel: LogEventLevel.Verbose,
            microsoftLevel: MsLogLevel.Trace)
    {
    }
}
