using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Deny-lists individual access tokens by their <c>jti</c> claim.
/// </summary>
/// <remarks>
/// A direct, single-purpose store rather than a <c>TokenManagerTypes</c>-dispatched service — the
/// same shape <c>JwtIssuanceResolver</c> already uses for issuance. There is exactly one kind of
/// token this host mints (<c>JwtTokenIssuer</c>), so there is nothing to select between.
/// </remarks>
public interface ITokenRevocationStore
{
    /// <summary>Deny-lists a token so it is rejected on every future presentation.</summary>
    /// <param name="jti">The token's <c>jti</c> claim.</param>
    /// <param name="expiresAt">When the token itself expires, for later housekeeping.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult> Revoke(Guid jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>Returns whether <paramref name="jti"/> has been revoked.</summary>
    /// <param name="jti">The token's <c>jti</c> claim.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<bool>> IsRevoked(Guid jti, CancellationToken cancellationToken = default);
}
