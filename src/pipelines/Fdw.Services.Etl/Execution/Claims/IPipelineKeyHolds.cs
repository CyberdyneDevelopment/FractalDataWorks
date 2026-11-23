using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// The hold on a key (FDW-6939): a person stops the waiting runs on a key (one conversation) from starting, and later lets them go. The key is the key a pipeline states, in the tenant of the caller; the
/// hold is a row in the operational database, so it survives a restart and both copies of a host see it.
/// </summary>
public interface IPipelineKeyHolds
{
    /// <summary>Places a hold on a key.</summary>
    /// <param name="tenantId">The tenant of the caller.</param>
    /// <param name="key">The key, as the pipeline's template expands it.</param>
    /// <param name="heldBy">The durable id of the person who places the hold.</param>
    /// <param name="reason">Why the key is held; stated, never assumed.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The hold, or a coded refusal: the key is already held (naming who holds it), no reason is stated, or the host names no operational connection.</returns>
    Task<IGenericResult<PipelineKeyHold>> Place(Guid tenantId, string key, Guid heldBy, string reason, CancellationToken cancellationToken = default);

    /// <summary>Releases the hold on a key, which promotes the waiting runs it was stopping.</summary>
    /// <param name="tenantId">The tenant of the caller.</param>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Who had placed it and the runs promoted, or a coded refusal when the key is not held.</returns>
    Task<IGenericResult<PipelineKeyHoldReleased>> Release(Guid tenantId, string key, CancellationToken cancellationToken = default);
}
