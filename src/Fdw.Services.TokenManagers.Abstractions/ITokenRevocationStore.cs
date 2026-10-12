using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Deny-lists access tokens: one token by its <c>jti</c> claim, or every token issued to a user up to a
/// moment.
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

    /// <summary>
    /// Ends every live session of <paramref name="userId"/>: each token issued to them at or before
    /// <paramref name="revokedAt"/> is rejected on its next presentation.
    /// </summary>
    /// <param name="userId">The user, as carried in their tokens' <c>sub</c> claim.</param>
    /// <param name="revokedAt">The cut-off. A token issued after it is unaffected.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// For a change to what the user may do that their already-issued tokens would otherwise keep
    /// granting until they expire — revoking one of their roles.
    /// </remarks>
    Task<IGenericResult> RevokeAll(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether a token has been revoked, either by its own <c>jti</c> or by a
    /// <see cref="RevokeAll"/> cut-off for its subject at or after the moment it was issued.
    /// </summary>
    /// <param name="jti">The token's <c>jti</c> claim.</param>
    /// <param name="userId">The token's <c>sub</c> claim.</param>
    /// <param name="issuedAt">The token's <c>iat</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<bool>> IsRevoked(Guid jti, Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken = default);
}
