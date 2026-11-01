using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.DataVault.Abstractions;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Narrow per-domain interface for the set-password link vault (FDW-941). The raw token is minted by the
/// service edge; the vault peppers and stores it and finds a candidate by its peppered hash, returning
/// only who it belongs to -- never a hash or a raw token.
/// </summary>
/// <remarks>
/// One live link per user: <see cref="Create"/> revokes the user's earlier unused links before storing
/// the new one. <see cref="Redeem"/> is atomic: the first caller to present a live token uses it up, and
/// every later attempt finds it used.
/// </remarks>
public interface IPasswordLinkVault : IDataVault
{
    /// <summary>Revokes the user's unused links, then peppers and stores the new one.</summary>
    /// <param name="userId">The user whose password the link sets.</param>
    /// <param name="rawToken">The freshly minted raw token, peppered and stored by the vault.</param>
    /// <param name="expiresAt">When the link stops working.</param>
    /// <param name="createdBy">The administrator creating it.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The new link's id.</returns>
    Task<IGenericResult<Guid>> Create(
        Guid userId, string rawToken, DateTimeOffset expiresAt, Guid createdBy, CancellationToken cancellationToken = default);

    /// <summary>Uses up the live link the candidate token names, in one atomic step.</summary>
    /// <param name="rawToken">The token from the link.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>Whether a live link matched and, if so, whose; no secret material.</returns>
    Task<IGenericResult<PasswordLinkRedemption>> Redeem(string rawToken, CancellationToken cancellationToken = default);
}
