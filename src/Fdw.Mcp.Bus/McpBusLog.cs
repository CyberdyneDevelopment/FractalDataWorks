using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Mcp.Bus;

/// <summary>
/// MessageLogging methods for the MCP event bus. EventIds are categorized numbers, not a contiguous
/// block: 1xxxx informational, 7xxxx dependency/IO, 8xxxx timeout, 9xxxx unexpected.
/// </summary>
[MessageLoggingTypeCode("BUS")]
public static partial class McpBusLog
{
    /// <summary>Logs a successful event publish.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Published event {eventId} topic {topic}")]
    public static partial IGenericMessage EventPublished(ILogger logger, ulong eventId, string topic);

    /// <summary>Logs that the file event-log sink is disabled because no directory is configured.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "FileEventLogSink disabled (no FileLogDirectory configured).")]
    public static partial IGenericMessage FileSinkDisabled(ILogger logger);

    /// <summary>Logs that appending an event to a log file failed.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to append event {eventId} to log file.")]
    public static partial IGenericMessage FileAppendFailed(ILogger logger, ulong eventId, Exception exception);

    /// <summary>Logs that a tool source is starting.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Starting MCP tool source: {server}")]
    public static partial IGenericMessage ToolSourceStarting(ILogger logger, string server);

    /// <summary>Logs that a tool source threw while stopping.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Warning,
        Message = "Tool source {server} stop threw")]
    public static partial IGenericMessage ToolSourceStopThrew(ILogger logger, string server, Exception exception);

    /// <summary>Logs that a tool source threw while disposing.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning,
        Message = "Tool source {server} dispose threw")]
    public static partial IGenericMessage ToolSourceDisposeThrew(ILogger logger, string server, Exception exception);

    /// <summary>Logs that a stdio bridge tool invocation failed.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Stdio bridge {server}/{tool} failed")]
    public static partial IGenericMessage StdioBridgeFailed(ILogger logger, string server, string tool, Exception exception);

    /// <summary>Logs that a stdio bridge discarded a non-JSON line.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Warning,
        Message = "Stdio bridge {server}: non-JSON line discarded: {line}")]
    public static partial IGenericMessage StdioBridgeNonJsonDiscarded(ILogger logger, string server, string line, Exception exception);

    /// <summary>Logs that an in-proc tool source threw.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "In-proc tool source {server}/{tool} threw")]
    public static partial IGenericMessage InProcToolSourceThrew(ILogger logger, string server, string tool, Exception exception);

    /// <summary>Logs that Process.Kill raced with the process exiting on its own.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Process kill raced with process exit; process has already exited.")]
    public static partial IGenericMessage ProcessKillRaceCondition(ILogger logger, Exception exception);
}
