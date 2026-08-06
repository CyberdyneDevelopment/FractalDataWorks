using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Connections.Abstractions.Logging;

/// <summary>
/// Static logger class for Connection operations using MessageLogging infrastructure.
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS6")]
public static partial class ConnectionLogger
{
    /// <summary>
    /// Logs when translation of a command fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Translation failed")]
    public static partial IGenericMessage TranslationFailed(ILogger logger);

    /// <summary>
    /// Logs when execution of a command fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Execution failed")]
    public static partial IGenericMessage ExecutionFailed(ILogger logger);

    /// <summary>
    /// Logs when a translator for a command type is not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandType">The command type that was not found.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "No translator found for command type '{commandType}'")]
    public static partial IGenericMessage TranslatorNotFound(ILogger logger, string commandType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace-Level Diagnostic Events (3004-3007)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces entry into ConnectionBase.Execute with container.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Entering ConnectionBase.Execute<T> for command type '{commandType}'")]
    public static partial IGenericMessage TraceExecuteWithContainerEntry(ILogger logger, string commandType);

    /// <summary>
    /// Traces entry into ConnectionBase.Execute (non-generic) with container.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "Entering ConnectionBase.Execute for command type '{commandType}'")]
    public static partial IGenericMessage TraceExecuteEntry(ILogger logger, string commandType);

    /// <summary>
    /// Traces entry into ConnectionBase.Execute without container (error path).
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Entering ConnectionBase.Execute without container (unsupported)")]
    public static partial IGenericMessage TraceExecuteNoContainerEntry(ILogger logger);
}
