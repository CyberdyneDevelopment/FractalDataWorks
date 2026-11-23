using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Issues, reads and revokes the stored grants of runs (FDW-6144), in the caller's tenant. A schedule and an answer request store a grant's id; the grant is the one place that says what runs may do as a
/// person while they are away and until when. Every refusal is a coded message.
/// </summary>
public interface IRunGrants
{
    /// <summary>Issues a grant for the caller, who is the person the runs will act as.</summary>
    /// <param name="permissions">What runs may do under the grant; at least one.</param>
    /// <param name="expiresAt">When the grant ends; later than now.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The grant; or the refusal when the caller is no person, no permission is stated or the end is not in the future.</returns>
    Task<IGenericResult<RunGrant>> Issue(IReadOnlyCollection<string> permissions, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>Reads a grant, revoked or not, so a caller can say it was revoked and not that it never was.</summary>
    /// <param name="grantId">The grant.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The grant; or the refusal when there is none in the caller's tenant.</returns>
    Task<IGenericResult<RunGrant>> Read(Guid grantId, CancellationToken cancellationToken = default);

    /// <summary>Takes a grant back: it allows nothing from now on.</summary>
    /// <param name="grantId">The grant.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the refusal when there is no such grant.</returns>
    Task<IGenericResult> Revoke(Guid grantId, CancellationToken cancellationToken = default);
}
