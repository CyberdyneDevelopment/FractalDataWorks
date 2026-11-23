using System;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Makes the origin a run's record states (FDW-6519): the host and slot are this process's, the person and the run mode are the run's.
/// </summary>
/// <remarks>
/// One source for every writer of a run row (the Queued runner, the held-run store, the trigger of a host), so a row cannot be written without them.
/// </remarks>
public interface IPipelineRunOrigins
{
    /// <summary>Makes the origin of a run.</summary>
    /// <param name="actingPersonId">The person the run acts as, or null for a run that acts as a tenant.</param>
    /// <param name="runMode">The name of the run mode that runs it.</param>
    /// <returns>The origin, or a coded failure when the run mode or this host's name is not stated.</returns>
    IGenericResult<PipelineRunOrigin> Of(Guid? actingPersonId, string? runMode);
}
