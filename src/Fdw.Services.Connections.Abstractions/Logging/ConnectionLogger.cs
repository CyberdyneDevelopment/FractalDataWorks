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

    /// <summary>Traces a command with no translator being run as the simpler commands it expands into.</summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "No translator for command type '{commandType}'; running its {commandCount} expanded commands")]
    public static partial IGenericMessage TraceExpansionStarted(ILogger logger, string commandType, int commandCount);

    /// <summary>Traces a command that assembles its own result running its expanded commands, in one transaction or in order.</summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "Command type '{commandType}' assembles its own result from its expanded commands; in one transaction: {inTransaction}")]
    public static partial IGenericMessage TraceExpansionTransaction(ILogger logger, string commandType, bool inTransaction);

    /// <summary>Logs a command that expands into commands answering rows, run as one that answers a row count.</summary>
    [MessageLogging(
        EventId = 90005,
        Level = LogLevel.Error,
        Message = "Command type '{commandType}' expands into commands that answer rows and does not assemble its own result, so it cannot be run for a row count")]
    public static partial IGenericMessage ExpansionNeedsAssembly(ILogger logger, string commandType);

    /// <summary>Logs a command that expands into commands answering a row count, run for a result of another type.</summary>
    [MessageLogging(
        EventId = 90004,
        Level = LogLevel.Error,
        Message = "Command type '{commandType}' expands into commands that answer a row count, and a result of type '{resultType}' was asked for")]
    public static partial IGenericMessage ExpansionResultNotCount(ILogger logger, string commandType, string resultType);

    /// <summary>Logs a command that returns columns, run for a result of a type that is not the list of returned rows.</summary>
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "Command type '{commandType}' names columns to return, which are answered as a list of row dictionaries, and a result of type '{resultType}' was asked for")]
    public static partial IGenericMessage ReturnedColumnsResultNotRows(ILogger logger, string commandType, string resultType);
}
