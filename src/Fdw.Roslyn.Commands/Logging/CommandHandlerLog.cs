using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for the command handler — the one place every command passes through.
/// </summary>
/// <remarks>
/// Failure numbers mirror the RoslynResultCodes entry the handler returns for that condition, so a log
/// line and the returned IGenericResult share a Code (HANDLER-60002 IS
/// <c>RoslynResultCodes.TranslatorNotFound</c>). The purely informational methods return nothing to a
/// caller, so they draw from category 1 per RESULTCODE-CATALOG.md — 5 digits, Category = Id / 10000,
/// with the 1..9999 band explicitly invalid.
///
/// This class previously used raw <c>[LoggerMessage]</c> with EventIds 4030-4036, which sit in that
/// invalid band and so belong to no category at all.
/// </remarks>
[MessageLoggingTypeCode("HANDLER")]
public static partial class CommandHandlerLog
{
    /// <summary>Trace: a command is about to be dispatched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandName">The command being run.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12500, Level = LogLevel.Trace,
        Message = "Executing command '{commandName}'")]
    public static partial IGenericMessage CommandExecuting(ILogger logger, string commandName);

    /// <summary>Debug: a command completed successfully.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandName">The command that ran.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12501, Level = LogLevel.Debug,
        Message = "Command '{commandName}' executed successfully")]
    public static partial IGenericMessage CommandExecuted(ILogger logger, string commandName);

    /// <summary>Debug: a mutation was applied to the in-memory workspace.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandName">The command that mutated.</param>
    /// <param name="changedFiles">How many documents the mutation touched.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Carries the file count because "the workspace was updated" and "the workspace was updated with
    /// nothing" read identically otherwise, and the second is the interesting one.
    /// </remarks>
    [MessageLogging(EventId = 12502, Level = LogLevel.Debug,
        Message = "Workspace updated after '{commandName}': {changedFiles} document(s) changed in memory")]
    public static partial IGenericMessage WorkspaceUpdated(ILogger logger, string commandName, int changedFiles);

    /// <summary>Warning: a translator returned a failure, which the handler forwards unchanged.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandName">The command that failed.</param>
    /// <param name="code">The result code the translator returned.</param>
    /// <param name="detail">The translator's message.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// No mirrored number of its own: the code belongs to the translator's result, which is passed
    /// straight through. Logging it here is what makes the originating code visible at the boundary
    /// even when the caller only ever sees a flattened string.
    ///
    /// Both carried nullable rather than coalesced to "Unknown": a result that arrived with no code at
    /// all is a different — and more interesting — fact than one whose code is named Unknown, and the
    /// two must not render alike.
    /// </remarks>
    [MessageLogging(EventId = 12503, Level = LogLevel.Warning,
        Message = "Command '{commandName}' failed [{code}]: {detail}")]
    public static partial IGenericMessage CommandFailed(ILogger logger, string commandName, string? code, string? detail);

    /// <summary>Trace: the handler is committing pending in-memory changes to disk.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pending">How many documents differ from the baseline before the write.</param>
    /// <param name="deleteRemovedFiles">Whether removed documents will be deleted from disk.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12504, Level = LogLevel.Trace,
        Message = "Applying workspace changes: {pending} pending document(s), deleteRemovedFiles={deleteRemovedFiles}")]
    public static partial IGenericMessage ApplyingWorkspaceChanges(ILogger logger, int pending, bool deleteRemovedFiles);

    /// <summary>Information: pending changes reached disk.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="written">How many files were written.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12505, Level = LogLevel.Information,
        Message = "Wrote {written} file(s) to disk")]
    public static partial IGenericMessage WorkspaceChangesWritten(ILogger logger, int written);

    /// <summary>Debug: there was nothing pending, so nothing was written.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Deliberately distinct from <see cref="PendingChangesVanished"/>. Writing nothing because there
    /// was nothing to write is a correct no-op; writing nothing when work was pending is data loss.
    /// Both produce a success result and the same "Wrote 0 file(s)" summary, so the log line is the only
    /// thing that tells them apart.
    /// </remarks>
    [MessageLogging(EventId = 12506, Level = LogLevel.Debug,
        Message = "No pending changes — nothing to write")]
    public static partial IGenericMessage NothingPendingToWrite(ILogger logger);

    /// <summary>Critical: changes were pending, the write reported success, and no file was written.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pending">How many documents were pending immediately before the write.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Critical because it is silent data loss: the caller is told the commit succeeded and the refactor
    /// is gone. This is the observed signature of a workspace evicted between the mutation and the
    /// commit — eviction discards the in-memory solution, the commit then finds nothing to do, and both
    /// calls return success. Nothing in the returned result distinguishes it from a genuine no-op, which
    /// is why it is logged at the level that gets noticed.
    /// </remarks>
    [MessageLogging(EventId = 91020, Level = LogLevel.Critical,
        Message = "PENDING CHANGES LOST: {pending} document(s) were pending, the write reported success, and 0 file(s) were written")]
    public static partial IGenericMessage PendingChangesVanished(ILogger logger, int pending);

    /// <summary>Information: the baseline was advanced to the current solution.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12507, Level = LogLevel.Information,
        Message = "Baseline advanced to the current solution")]
    public static partial IGenericMessage BaselineAdvanced(ILogger logger);

    /// <summary>Information: the change ledger was cleared because the caller asked by name.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// The ledger is the record a migration guide is built from, and this is the only path that discards
    /// it. When it turns up empty, this line is the difference between "it was cleared, here is when"
    /// and re-deriving that from source.
    /// </remarks>
    [MessageLogging(EventId = 12508, Level = LogLevel.Information,
        Message = "Change ledger cleared at the caller's request")]
    public static partial IGenericMessage LedgerClearedByRequest(ILogger logger);

    /// <summary>Debug: a mutation was recorded in the change ledger.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandName">The command that mutated.</param>
    /// <param name="hasReason">Whether the caller supplied a reason.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12509, Level = LogLevel.Debug,
        Message = "Ledger recorded mutation from '{commandName}' (reason supplied: {hasReason})")]
    public static partial IGenericMessage MutationRecorded(ILogger logger, string commandName, bool hasReason);

    /// <summary>Error: the caller passed a null command.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>Mirrors <c>RoslynResultCodes.CommandCannotBeNull</c> (21000).</remarks>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "Refused — the command was null")]
    public static partial IGenericMessage CommandWasNull(ILogger logger);

    /// <summary>Error: no translator is registered for the command type.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandName">The command that was asked for.</param>
    /// <param name="commandType">Its CLR type, which is the registry key.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Mirrors <c>RoslynResultCodes.TranslatorNotFound</c> (60002). Category 6 is right: a missing
    /// translator is a wiring fault, not a bad request — the same command works once the registry is
    /// hydrated.
    /// </remarks>
    [MessageLogging(EventId = 60002, Level = LogLevel.Error,
        Message = "No translator registered for command '{commandName}' (type: {commandType})")]
    public static partial IGenericMessage TranslatorNotFound(ILogger logger, string commandName, string commandType);

    /// <summary>Information: the command was cancelled.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The cancellation.</param>
    /// <param name="commandName">The command that was cancelled.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>Mirrors <c>RoslynResultCodes.CommandExecutionCancelled</c> (91000).</remarks>
    [MessageLogging(EventId = 91000, Level = LogLevel.Information,
        Message = "Command '{commandName}' was cancelled")]
    public static partial IGenericMessage CommandCancelled(ILogger logger, Exception exception, string commandName);

    /// <summary>Error: a translator threw rather than returning a failure.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that escaped.</param>
    /// <param name="commandName">The command that threw.</param>
    /// <param name="errorMessage">The exception's message.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Mirrors <c>RoslynResultCodes.CommandExecutionFailed</c> (91001). The exception goes to the logger
    /// as an exception, not as a message string: the returned result carries only the message, so the
    /// stack trace exists in exactly one place and this is it.
    /// </remarks>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Command '{commandName}' threw: {errorMessage}")]
    public static partial IGenericMessage CommandException(ILogger logger, Exception exception, string commandName, string errorMessage);
}
