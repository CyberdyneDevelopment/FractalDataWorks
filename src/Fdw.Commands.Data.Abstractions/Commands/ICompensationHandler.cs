using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Handles compensation (rollback) when a composite command fails.
/// </summary>
public interface ICompensationHandler
{
    /// <summary>
    /// Gets the unique identifier for this compensation handler.
    /// </summary>
    string HandlerId { get; }

    /// <summary>
    /// Gets the commands that execute to compensate (rollback) changes.
    /// These typically reverse the effects of successful commands before the failure.
    /// </summary>
    IReadOnlyList<IDataCommand> CompensationCommands { get; }

    /// <summary>
    /// Executes the compensation logic.
    /// </summary>
    /// <param name="failedCommandIndex">Index of the command that failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the compensation attempt.</returns>
    Task<IGenericResult> Compensate(int failedCommandIndex, CancellationToken cancellationToken = default);
}