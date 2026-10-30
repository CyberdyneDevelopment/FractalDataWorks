using System;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Execution;

/// <summary>
/// Reaches the run an execution row stands for. Moving the row to Cancelled or Paused records a decision; this makes the run
/// obey it, so a person told "Cancelled" is not left with a run that goes on to the end (FDW-2950).
/// </summary>
/// <remarks>
/// A host that runs executions provides it. The control answers for the runs its own host holds: a run that is waiting for its
/// turn, running or paused. A run it does not hold (not started yet, already finished, or held by another host) is a failure,
/// never a quiet success, so the caller does not record a state the run never reached.
/// </remarks>
public interface IExecutionRunControl
{
    /// <summary>Stops the run: a run waiting for its turn leaves the queue and a running run is cancelled.</summary>
    /// <param name="executionId">The execution row's id.</param>
    /// <returns>Success once the run was told to stop; a failure when this host holds no such run.</returns>
    IGenericResult Cancel(Guid executionId);

    /// <summary>Holds the run before its next batch of rows until it is resumed or cancelled.</summary>
    /// <param name="executionId">The execution row's id.</param>
    /// <returns>Success once the run was told to hold; a failure when this host holds no such run, or it is already paused.</returns>
    IGenericResult Pause(Guid executionId);

    /// <summary>Lets a paused run go on.</summary>
    /// <param name="executionId">The execution row's id.</param>
    /// <returns>Success once the run was released; a failure when this host holds no such run, or it is not paused.</returns>
    IGenericResult Resume(Guid executionId);
}
