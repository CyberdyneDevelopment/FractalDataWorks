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
    /// Logs a data command that carries a command timeout sent to a kind of connection that cannot apply one (FDW-3324).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="owner">Who set the limit, in the words a failure uses; empty when the command does not say.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(
        EventId = 62034,
        Level = LogLevel.Error,
        Message = "A command carrying a command timeout ({owner}) was sent to a kind of connection that cannot apply one, so it is not run",
        ClientMessage = "This kind of connection cannot apply the command timeout that was set ({owner}), so the command was not run")]
    public static partial IGenericMessage CommandTimeoutNotApplied(ILogger logger, string owner);

    /// <summary>
    /// Logs a command that expands into simpler commands and carries a command timeout (FDW-3334): the steps are commands of their own,
    /// so the limit cannot reach them and the command is refused instead of run without it.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandType">The type of the command that declares the expansion.</param>
    /// <param name="owner">Who set the limit, in the words a failure uses; empty when the command does not say.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(
        EventId = 62035,
        Level = LogLevel.Error,
        Message = "Command type '{commandType}' expands into other commands and carries a command timeout ({owner}) that its steps cannot take, so it is not run",
        ClientMessage = "The command timeout that was set ({owner}) cannot be applied to a command that runs as several, so the command was not run")]
    public static partial IGenericMessage ExpansionCannotCarryCommandTimeout(ILogger logger, string commandType, string owner);

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

    /// <summary>Traces whether a command that expands into commands answering a row count runs them in one transaction.</summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Trace,
        Message = "Command type '{commandType}' runs its expanded commands for a row count; in one transaction: {inTransaction}")]
    public static partial IGenericMessage TraceCountedExpansionTransaction(ILogger logger, string commandType, bool inTransaction);

    /// <summary>
    /// Warns that a connection that cannot open a transaction (an HTTP or Roslyn workspace connection) runs the commands a command
    /// expands into one after another: a step that fails leaves the steps before it done, with nothing to roll them back.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Warning,
        Message = "Connection '{connectionName}' cannot open a transaction, so the commands command type '{commandType}' expands into run one after another; a failed step leaves the steps before it in place")]
    public static partial IGenericMessage ExpansionRunsWithoutTransaction(ILogger logger, string connectionName, string commandType);

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

    /// <summary>Logs a BulkWriteAnswer asked of a command that does not report its unmatched keys (FDW-4988).</summary>
    [MessageLogging(
        EventId = 20002,
        Level = LogLevel.Error,
        Message = "A BulkWriteAnswer was asked of command type '{commandType}', which does not report its unmatched keys; state its UnmatchedKeySample to ask for them")]
    public static partial IGenericMessage AnswerNotAskedFor(ILogger logger, string commandType);

    /// <summary>Logs a write that reports its unmatched keys run for a result other than a BulkWriteAnswer (FDW-4988).</summary>
    [MessageLogging(
        EventId = 20003,
        Level = LogLevel.Error,
        Message = "Command type '{commandType}' reports its unmatched keys, which are answered as a BulkWriteAnswer, and a result of type '{resultType}' was asked for")]
    public static partial IGenericMessage UnmatchedKeysResultNotAnswer(ILogger logger, string commandType, string resultType);

    /// <summary>Logs a write that reports its unmatched keys on a kind with no translator for it (FDW-4988).</summary>
    [MessageLogging(
        EventId = 90006,
        Level = LogLevel.Error,
        Message = "Command type '{commandType}' reports its unmatched keys, and connection '{connectionName}' has no translator for it; a command expanded row by row cannot tell a key that matched no row")]
    public static partial IGenericMessage UnmatchedKeysNeedTranslator(ILogger logger, string commandType, string connectionName);

    /// <summary>Logs a connection asked to read result sets apart that cannot (FDW-4988).</summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Connection '{connectionName}' cannot read a batch's result sets apart, which a write that reports its unmatched keys answers")]
    public static partial IGenericMessage ResultSetsNotSupported(ILogger logger, string connectionName);
}
