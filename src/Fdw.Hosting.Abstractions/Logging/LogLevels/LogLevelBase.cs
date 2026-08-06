using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fdw.Hosting.Abstractions.Logging;

/// <summary>
/// Base class for log level TypeOptions.
/// </summary>
// Why: pure data holder — constructors only assign properties, no branching logic; every
// concrete TypeOption in this hierarchy (DebugLogLevel, ErrorLogLevel, etc.) is already excluded.
[ExcludeFromCodeCoverage]
public abstract class LogLevelBase : TypeOptionBase<int, LogLevelBase>, ILogLevel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogLevelBase"/> class for Empty sentinel.
    /// </summary>
    protected LogLevelBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
    {
        SerilogLevel = LogEventLevel.Verbose;
        MicrosoftLevel = MsLogLevel.Trace;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogLevelBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The log level name.</param>
    /// <param name="description">Description of when to use this level.</param>
    /// <param name="serilogLevel">The Serilog LogEventLevel.</param>
    /// <param name="microsoftLevel">The Microsoft LogLevel.</param>
    protected LogLevelBase(
        int id,
        string name,
        string description,
        LogEventLevel serilogLevel,
        MsLogLevel microsoftLevel)
        : base(id, name, $"LogLevels:{name}", name, description, "Logging")
    {
        SerilogLevel = serilogLevel;
        MicrosoftLevel = microsoftLevel;
    }

    /// <inheritdoc/>
    public LogEventLevel SerilogLevel { get; }

    /// <inheritdoc/>
    public MsLogLevel MicrosoftLevel { get; }
}
