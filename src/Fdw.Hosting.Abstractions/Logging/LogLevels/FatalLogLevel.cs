using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Fatal log level - critical failures requiring immediate attention.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LogLevels), "Fatal", RestrictToCurrentCompilation = true)]
public sealed class FatalLogLevel : LogLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FatalLogLevel"/> class.
    /// </summary>
    public FatalLogLevel()
        : base(
            id: 5,
            name: "Fatal",
            description: "Critical failures that cause application termination or require immediate attention",
            serilogLevel: LogEventLevel.Fatal,
            microsoftLevel: MsLogLevel.Critical)
    {
    }
}
