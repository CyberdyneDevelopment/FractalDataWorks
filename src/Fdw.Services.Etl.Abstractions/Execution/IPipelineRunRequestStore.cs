using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Keeps what an ETL host needs to take an accepted pipeline run up again after a restart (FDW-3216): the request the run was accepted with, and the time
/// a held run is held until. Both are events on the run's execution record, so the store needs no table of its own.
/// </summary>
public interface IPipelineRunRequestStore
{
    /// <summary>Records the request of a run the host has accepted, on the run's execution record.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A failure naming why when the request could not be recorded, for example a parameter of a type that cannot be kept; the run is then not accepted.</returns>
    Task<IGenericResult> Record(PipelineExecutionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Records that a run is held until a time.</summary>
    /// <param name="request">The request of the held run.</param>
    /// <param name="heldUntil">When the hold ends.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A failure naming why when the hold could not be recorded.</returns>
    Task<IGenericResult> RecordHeld(PipelineExecutionRequest request, DateTimeOffset heldUntil, CancellationToken cancellationToken = default);

    /// <summary>Removes the stored request of a run that has started, been refused or been cancelled.</summary>
    /// <param name="executionId">The run's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A failure naming why when the request could not be removed; it is then removed the next time the host reads the stored requests, since its run has moved on.</returns>
    Task<IGenericResult> Forget(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a run that was running when the host stopped as failed, naming the new run, makes a new run with the same request (FDW-3236), and stores the
    /// new run's request with one more restart than the old run had.
    /// </summary>
    /// <param name="run">The run that was running.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request of the new run, to put in the queue; a failure naming why when the new run could not be made.</returns>
    Task<IGenericResult<PipelineExecutionRequest>> StartAgain(InterruptedRun run, CancellationToken cancellationToken = default);

    /// <summary>Closes a run that was running when the host stopped as failed, giving the reason it is not started again, and removes its stored request.</summary>
    /// <param name="run">The run that was running.</param>
    /// <param name="reason">Why it is not started again.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A failure naming why when the run could not be closed.</returns>
    Task<IGenericResult> GiveUp(InterruptedRun run, string reason, CancellationToken cancellationToken = default);

    /// <summary>Reads the runs that were accepted and not started, oldest first, for the host to take up again.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The requests to take up and how many unstarted records have no request; a failure when the records cannot be read.</returns>
    Task<IGenericResult<PipelineRunRecovery>> Recover(CancellationToken cancellationToken = default);
}
