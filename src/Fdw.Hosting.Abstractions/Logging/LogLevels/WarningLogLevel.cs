using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Warning log level - potential issues that may require attention.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LogLevels), "Warning", RestrictToCurrentCompilation = true)]
public sealed class WarningLogLevel : LogLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningLogLevel"/> class.
    /// </summary>
    public WarningLogLevel()
        : base(
            id: 3,
            name: "Warning",
            description: "Potential issues that may require attention but don't prevent operation",
            serilogLevel: LogEventLevel.Warning,
            microsoftLevel: MsLogLevel.Warning)
    {
    }
}
