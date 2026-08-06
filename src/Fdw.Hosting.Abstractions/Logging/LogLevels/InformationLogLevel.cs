using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Information log level - general operational events.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LogLevels), "Information", RestrictToCurrentCompilation = true)]
public sealed class InformationLogLevel : LogLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InformationLogLevel"/> class.
    /// </summary>
    public InformationLogLevel()
        : base(
            id: 2,
            name: "Information",
            description: "General operational events that highlight application progress",
            serilogLevel: LogEventLevel.Information,
            microsoftLevel: MsLogLevel.Information)
    {
    }
}
