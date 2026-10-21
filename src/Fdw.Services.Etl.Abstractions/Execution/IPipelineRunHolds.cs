using System;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Keeps the requests of the pipeline runs that wait for approval (FDW-1937), until someone approves or denies the run.
/// </summary>
/// <remarks>
/// <para>
/// A pipeline that requires approval is judged when it is started and then held instead of queued. The request that would have been
/// queued carries what the run needs (its typed values, roles, tenant and test rows) and that exists nowhere else, so it is kept here.
/// The values never reach the run's record or its events, so a value its author did not opt in to show stays private.
/// </para>
/// <para>
/// Why memory: the queue the request would have entered is memory too, so a restart empties both. A hold that is gone at approval is
/// reported as lost, never recreated.
/// </para>
/// </remarks>
public interface IPipelineRunHolds
{
    /// <summary>Keeps the request of a run that waits for approval.</summary>
    /// <param name="request">The request that would have been queued.</param>
    /// <returns><see langword="true"/> when kept; <see langword="false"/> when a request is already held for the run.</returns>
    bool Hold(PipelineExecutionRequest request);

    /// <summary>Removes and returns the request held for a run.</summary>
    /// <param name="executionId">The run's id.</param>
    /// <returns>The request, or <see langword="null"/> when none is held (it was already taken, or the host restarted).</returns>
    PipelineExecutionRequest? Take(Guid executionId);
}
