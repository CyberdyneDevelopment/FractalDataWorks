using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Debug log level - detailed information useful during development.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LogLevels), "Debug", RestrictToCurrentCompilation = true)]
public sealed class DebugLogLevel : LogLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogLevel"/> class.
    /// </summary>
    public DebugLogLevel()
        : base(
            id: 1,
            name: "Debug",
            description: "Detailed information useful during development and debugging",
            serilogLevel: LogEventLevel.Debug,
            microsoftLevel: MsLogLevel.Debug)
    {
    }
}
