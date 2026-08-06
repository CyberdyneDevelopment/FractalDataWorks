using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Workspace.Results;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands;

/// <summary>
/// Default implementation of <see cref="IRoslynCommandHandler"/>.
/// Orchestrates command execution between workspace and translators.
/// </summary>
public sealed class RoslynCommandHandler : IRoslynCommandHandler
{
    private readonly IRoslynWorkspace _workspace;
    private readonly ITranslatorRegistry _translators;
    private readonly IChangeLedger _ledger;
    private readonly ILogger<RoslynCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynCommandHandler"/> class.
    /// </summary>
    /// <param name="workspace">The workspace to execute commands against.</param>
    /// <param name="translators">The translator registry.</param>
    /// <param name="ledger">Optional change ledger.</param>
    /// <param name="logger">Optional logger.</param>
    public RoslynCommandHandler(
        IRoslynWorkspace workspace,
        ITranslatorRegistry translators,
        IChangeLedger? ledger = null,
        ILogger<RoslynCommandHandler>? logger = null)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _translators = translators ?? throw new ArgumentNullException(nameof(translators));
        _ledger = ledger ?? NullChangeLedger.Instance;
        _logger = logger ?? NullLogger<RoslynCommandHandler>.Instance;
    }

    /// <inheritdoc/>
    // MA0051: Method length acceptable - command handler pattern with translator lookup, execution, and error handling
#pragma warning disable MA0051 // Method is too long
    public async Task<IGenericResult<TResult>> Execute<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRoslynCommand
        where TResult : IRoslynCommandResult
#pragma warning restore MA0051
    {
        if (command is null)
        {
            CommandHandlerLog.CommandWasNull(_logger);
            return GenericResult<TResult>.Failure(
                RoslynResultCodes.ByName("CommandCannotBeNull"));
        }

        var commandName = command.Name;
        CommandHandlerLog.CommandExecuting(_logger, commandName);

        // Get translator for command type
        var translatorResult = _translators.GetTranslator<TCommand, TResult>();
        if (!translatorResult.IsSuccess)
        {
            CommandHandlerLog.TranslatorNotFound(_logger, commandName, typeof(TCommand).Name);
            return GenericResult<TResult>.Failure(
                RoslynResultCodes.ByName("TranslatorNotFound"),
                ResultDetails.Create("Message", translatorResult.CurrentMessage ?? "Unknown translator"));
        }

        var translator = translatorResult.Value!;

        // Get current solution from workspace
        var solution = _workspace.CurrentSolution;

        try
        {
            // Execute stateless translator
            var result = await translator.Translate(command, solution, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                CommandHandlerLog.CommandFailed(_logger, commandName, result.Code?.Name, result.CurrentMessage);
                return result;
            }

            // If mutation, apply to workspace
            if (result.Value!.IsMutation && result.Value.NewSolution is not null)
            {
                _workspace.UpdateSolution(result.Value.NewSolution);
                RecordMutation(commandName, result.Value, ReasonOf(command));
                CommandHandlerLog.WorkspaceUpdated(
                    _logger, commandName, result.Value is MutationResult mutation ? mutation.ChangedFiles.Count : 0);
            }

            CommandHandlerLog.CommandExecuted(_logger, commandName);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            CommandHandlerLog.CommandCancelled(_logger, ex, commandName);
            return GenericResult<TResult>.Failure(
                RoslynResultCodes.ByName("CommandExecutionCancelled"));
        }
#pragma warning disable CA1031 // Do not catch general exception types - handler should not throw
        catch (Exception ex)
        {
            CommandHandlerLog.CommandException(_logger, ex, commandName, ex.Message);
            return GenericResult<TResult>.Failure(
                RoslynResultCodes.ByName("CommandExecutionFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc/>
    // MA0051: Method length acceptable - non-generic command handler with translator lookup, execution, and error handling
#pragma warning disable MA0051 // Method is too long
    public async Task<IGenericResult<IRoslynCommandResult>> Execute(
        IRoslynCommand command,
        CancellationToken cancellationToken = default)
#pragma warning restore MA0051
    {
        if (command is null)
        {
            CommandHandlerLog.CommandWasNull(_logger);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("CommandCannotBeNull"));
        }

        var commandName = command.Name;
        var commandType = command.GetType();

        CommandHandlerLog.CommandExecuting(_logger, commandName);

        // Get translator for command type
        var translatorResult = _translators.GetTranslator(commandType);
        if (!translatorResult.IsSuccess)
        {
            CommandHandlerLog.TranslatorNotFound(_logger, commandName, commandType.Name);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("TranslatorNotFound"),
                ResultDetails.Create("Message", translatorResult.CurrentMessage ?? "Unknown translator"));
        }

        var translator = translatorResult.Value!;

        // Get current solution from workspace
        var solution = _workspace.CurrentSolution;

        // Inject the current baseline into the command when the command exposes a
        // settable BaselineSolution property. Baseline-aware commands (GetBaseline,
        // CompareToBaseline, RevertToBaseline) cannot reach the workspace themselves
        // because translators are stateless.
        InjectBaseline(command);

        // RestoreSnapshot needs its snapshot's Solution resolved from the workspace
        // by SnapshotId and stamped onto the command before the translator runs.
        InjectSnapshotSolution(command);

        // Ledger-aware commands (GetChangeLedger, WriteMigrationGuide) need the session's
        // change ledger, which the stateless translator cannot reach on its own.
        InjectLedger(command);

        try
        {
            // Execute translator
            var result = await translator.Execute(command, solution, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                CommandHandlerLog.CommandFailed(_logger, commandName, result.Code?.Name, result.CurrentMessage);
                return result;
            }

            // If mutation, apply to workspace
            if (result.Value!.IsMutation && result.Value.NewSolution is not null)
            {
                _workspace.UpdateSolution(result.Value.NewSolution);
                RecordMutation(commandName, result.Value, ReasonOf(command));
                CommandHandlerLog.WorkspaceUpdated(
                    _logger, commandName, result.Value is MutationResult mutation ? mutation.ChangedFiles.Count : 0);
            }

            // Why these dispatch on the command's TYPE, not on its name: each of the four effects below
            // lives on the workspace, which a stateless translator cannot reach, so the handler performs
            // it. Matching on the literal string "SetBaseline" meant renaming a command — or adding a
            // second command that legitimately wants the same effect — silently stopped the effect from
            // happening, with a successful result and no log line to say so. The capability interfaces
            // put that in the type system, where a rename is a compile error.

            // The ONLY place the history is discarded, and it happens because someone asked for it by
            // name. Everything else that used to clear it — loading a solution, closing a workspace,
            // setting a baseline — had no business doing so.
            if (command is ILedgerClearingCommand)
            {
                _ledger.Clear();
                CommandHandlerLog.LedgerClearedByRequest(_logger);
            }

            // A baseline is "compare against here from now on". The ledger is "what has been done".
            // Wiping the history because someone moved the comparison point conflated two unrelated
            // things and silently destroyed the record the migration guide is built from.
            if (command is IBaselineSettingCommand)
            {
                _workspace.SetBaseline(_workspace.CurrentSolution);
                CommandHandlerLog.BaselineAdvanced(_logger);
            }

            // The translator mints a placeholder id because it cannot reach the workspace; the real
            // store happens here, and the result the caller receives is rebuilt around the real id.
            if (command is ISnapshotCreatingCommand creating)
            {
                CommandHandlerLog.CommandExecuted(_logger, commandName);
                return StoreCreatedSnapshot(creating, result.Value);
            }

            if (command is IWorkspaceCommitCommand commit)
            {
                return await ApplyPendingWorkspaceChanges(commandName, commit, cancellationToken).ConfigureAwait(false);
            }

            CommandHandlerLog.CommandExecuted(_logger, commandName);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            CommandHandlerLog.CommandCancelled(_logger, ex, commandName);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("CommandExecutionCancelled"));
        }
#pragma warning disable CA1031 // Do not catch general exception types - handler should not throw
        catch (Exception ex)
        {
            CommandHandlerLog.CommandException(_logger, ex, commandName, ex.Message);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("CommandExecutionFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
#pragma warning restore CA1031
    }

    // Why: the ledger records WHAT changed; the caller's Reason is the only thing that records WHY, and a
    // migration guide that cannot say which slice or issue caused a move is not auditable. Declared via
    // IReasonedCommand rather than probed for by name — a command that spelled the property differently
    // used to lose its reason silently, and the ledger entry simply had no WHY.
    private static string? ReasonOf(IRoslynCommand command) =>
        command is IReasonedCommand reasoned ? reasoned.Reason : null;

    private void RecordMutation(string commandName, IRoslynCommandResult resultValue, string? reason)
    {
        if (resultValue is MutationResult m)
        {
            var summary = string.IsNullOrWhiteSpace(reason) ? m.Summary : $"{m.Summary} — reason: {reason}";
            _ = _ledger.Record(commandName, summary, m.ChangedFiles, m.SymbolChanges, m.PathChanges);
            CommandHandlerLog.MutationRecorded(_logger, commandName, !string.IsNullOrWhiteSpace(reason));
        }
    }

    private void InjectLedger(IRoslynCommand command)
    {
        if (command is ILedgerAwareCommand ledgerAwareCommand)
        {
            ledgerAwareCommand.Ledger = _ledger;
        }
    }

    private void InjectBaseline(IRoslynCommand command)
    {
        if (command is IBaselineAwareCommand baselineAware)
        {
            baselineAware.BaselineSolution = _workspace.BaselineSolution;
        }
    }

    /// <summary>
    /// Performs the real snapshot store and returns a result carrying the real id.
    /// </summary>
    /// <remarks>
    /// Why a replacement rather than a patch: the translator cannot reach IRoslynWorkspace, so it mints a
    /// placeholder id. The previous code reached into the returned object with reflection and wrote
    /// SnapshotData.SnapshotId — a property declared <c>init</c>, i.e. deliberately immutable after
    /// construction. Reflection made that compile-time guarantee a lie, and any rename of the property
    /// silently reverted callers to a placeholder id that resolves to no snapshot. Building the result
    /// the caller receives is the same shape the commit path already uses.
    /// </remarks>
    private IGenericResult<IRoslynCommandResult> StoreCreatedSnapshot(
        ISnapshotCreatingCommand command,
        IRoslynCommandResult? resultValue)
    {
        if (string.IsNullOrWhiteSpace(command.SnapshotName))
        {
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("SnapshotNameRequired"));
        }

        var realId = _workspace.CreateSnapshot(command.SnapshotName, command.SnapshotDescription);

        // The translator's own counts are the ones taken against the solution it saw, so they are kept;
        // only the id it could not know is replaced.
        var placeholder = (resultValue as MutationResult<SnapshotData>)?.Data;

        return GenericResult<IRoslynCommandResult>.Success(
            new MutationResult<SnapshotData>(
                $"Created snapshot '{command.SnapshotName}'",
                _workspace.CurrentSolution,
                new SnapshotData
                {
                    SnapshotId = realId,
                    Name = command.SnapshotName,
                    Description = command.SnapshotDescription,
                    ProjectCount = placeholder?.ProjectCount ?? 0,
                    DocumentCount = placeholder?.DocumentCount ?? 0,
                    Restored = false,
                }));
    }

    private async Task<IGenericResult<IRoslynCommandResult>> ApplyPendingWorkspaceChanges(
        string commandName, IWorkspaceCommitCommand command, CancellationToken cancellationToken)
    {
        // Why: the translator cannot reach IRoslynWorkspace (see ApplyWorkspaceChangesTranslator), so the
        // handler performs the commit — which means it also has to carry the caller's delete decision.
        // Read off the interface: the reflection probe this replaces silently defaulted to false when the
        // property could not be found, so a caller who asked for deletion got a source file left behind
        // next to its moved copy — the duplicate-type build break the flag exists to prevent.
        var deleteRemovedFiles = command.DeleteRemovedFiles;

        // Why: captured BEFORE the write, because the write is what consumes it. Afterwards there is no
        // way to tell "nothing was pending" from "something was pending and is now gone" — the two
        // produce an identical success result, and only the second is a catastrophe.
        var pending = _workspace.GetChangesFromBaseline().Count;
        CommandHandlerLog.ApplyingWorkspaceChanges(_logger, pending, deleteRemovedFiles);

        var applyResult = await _workspace.ApplyChanges(deleteRemovedFiles, cancellationToken).ConfigureAwait(false);
        if (!applyResult.IsSuccess)
        {
            CommandHandlerLog.CommandFailed(_logger, commandName, applyResult.Code?.Name, applyResult.CurrentMessage);
            return applyResult.ToNewResult<IRoslynCommandResult>();
        }

        var written = applyResult.Value ?? Array.Empty<string>();

        if (written.Count > 0)
        {
            CommandHandlerLog.WorkspaceChangesWritten(_logger, written.Count);
        }
        else if (pending == 0)
        {
            CommandHandlerLog.NothingPendingToWrite(_logger);
        }
        else
        {
            CommandHandlerLog.PendingChangesVanished(_logger, pending);
        }

        CommandHandlerLog.CommandExecuted(_logger, commandName);
        return GenericResult<IRoslynCommandResult>.Success(
            new QueryResult<IReadOnlyList<string>>($"Wrote {written.Count} file(s) to disk", written));
    }

    private void InjectSnapshotSolution(IRoslynCommand command)
    {
        if (command is not ISnapshotRestoringCommand restoring) return;
        if (string.IsNullOrWhiteSpace(restoring.SnapshotId)) return;

        var lookup = _workspace.RestoreSnapshot(restoring.SnapshotId);
        if (lookup.IsSuccess && lookup.Value is not null)
        {
            restoring.SnapshotSolution = lookup.Value;
        }
    }
}
