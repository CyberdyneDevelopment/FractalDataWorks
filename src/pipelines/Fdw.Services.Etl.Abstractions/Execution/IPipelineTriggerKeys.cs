using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Remembers the key of every scheduled fire the ETL host has taken (FDW-4317), so a retry of one fire, after a 504 for example, is answered with the run the first call
/// started and never starts a second.
/// </summary>
public interface IPipelineTriggerKeys
{
    /// <summary>Claims a fire's key before its run is made.</summary>
    /// <param name="tenantId">The tenant the fire is for.</param>
    /// <param name="key">The fire's key.</param>
    /// <param name="fireTime">The time the fire was due.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A new claim, or the run an earlier call with the key started. A failure with a coded message when the fire is older than the retention (expired), when an earlier call
    /// with the key has not yet made its run (in progress), or when the key cannot be read or written; never "new" for a key that could not be told.
    /// </returns>
    Task<IGenericResult<PipelineTriggerKeyClaim>> Claim(Guid tenantId, string key, DateTimeOffset fireTime, CancellationToken cancellationToken = default);

    /// <summary>States which run a claimed key started.</summary>
    /// <param name="tenantId">The tenant the fire is for.</param>
    /// <param name="key">The fire's key.</param>
    /// <param name="executionId">The run the fire started.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A failure naming why when the run could not be bound to its key.</returns>
    Task<IGenericResult> Bind(Guid tenantId, string key, Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>Gives a claimed key back when its run was not made, so a retry can claim it.</summary>
    /// <param name="tenantId">The tenant the fire is for.</param>
    /// <param name="key">The fire's key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A failure naming why when the key could not be released.</returns>
    Task<IGenericResult> Release(Guid tenantId, string key, CancellationToken cancellationToken = default);
}
