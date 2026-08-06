using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Error log level - failures that prevent specific operations.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LogLevels), "Error", RestrictToCurrentCompilation = true)]
public sealed class ErrorLogLevel : LogLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorLogLevel"/> class.
    /// </summary>
    public ErrorLogLevel()
        : base(
            id: 4,
            name: "Error",
            description: "Failures that prevent specific operations from completing",
            serilogLevel: LogEventLevel.Error,
            microsoftLevel: MsLogLevel.Error)
    {
    }
}
