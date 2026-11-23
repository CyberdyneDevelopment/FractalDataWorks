using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>
/// An execution record a caller opened before it started the run (FDW-6131), with the values it already read against the pipeline's declared
/// parameters. The run mode queues that execution as it is: it opens no second record, reads no value again and records no parameter again.
/// </summary>
/// <param name="ExecutionId">The execution the caller opened.</param>
/// <param name="ResolvedParameters">The run's parameter values, read as the types the pipeline declares; null for a run given none, as its queued request carries them.</param>
public sealed record PipelineStartOpened(Guid ExecutionId, IReadOnlyDictionary<string, object?>? ResolvedParameters)
{
    /// <summary>
    /// Gets whether this start releases a run that was held (for approval) after the server kept its request (FDW-6137). The start's
    /// <see cref="PipelineStart.PersonId"/> is then the person who asked for the run, read from that kept request (null for a run asked for by no person),
    /// and the run acts as them, never as the caller releasing it: the releaser's authority covers the release alone.
    /// </summary>
    public bool ReleasesHeldRun { get; init; }

    /// <summary>
    /// Gets whether the person of this start is the one a schedule's stored grant names (FDW-6144): the server judged the grant (it has not ended, it allows what the run's roles carry) and the
    /// person (they may still act) before it opened the run. The start's <see cref="PipelineStart.PersonId"/> is then that person, and the run acts as them although its caller, the scheduler, is no
    /// person. Only the server's trigger states it, in the process that judged the grant.
    /// </summary>
    public bool ActsAsGrantedPerson { get; init; }
}
