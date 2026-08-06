using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.DataVault.Abstractions;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Narrow per-domain interface for the Personal Access Token vault. The raw token is minted by the
/// service edge (which owns the generator + policy); the vault peppers and stores it, and only ever
/// returns metadata — never a hash or raw token. <see cref="List"/> returns label/dates only.
/// </summary>
/// <remarks>
/// The freshly-minted raw token is passed in solely to be peppered inside the vault (the pepper is
/// vault-only); it is never stored in the clear, logged, or returned. The edge supplies the per-user
/// limit value (policy); the vault enforces it atomically with the insert (mechanism).
/// </remarks>
public interface IPatVault : IDataVault
{
    /// <summary>
    /// Peppers and stores the minted raw token (enforcing the active-token limit atomically) and
    /// returns the new token id. The raw token is never stored in the clear or returned by the vault.
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="rawToken">The freshly-minted raw token, peppered and stored by the vault.</param>
    /// <param name="label">The user-assigned label.</param>
    /// <param name="expiresAt">Optional UTC expiration; <c>null</c> for non-expiring.</param>
    /// <param name="maxActiveTokens">The per-user active-token limit the vault enforces before inserting.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<Guid>> Create(Guid userId, string rawToken, string label, DateTime? expiresAt, int maxActiveTokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Peppers the candidate raw token and looks it up; on a live, non-revoked, non-expired match
    /// returns the owning user and token id (no secret material) and best-effort touches LastUsedAt.
    /// </summary>
    /// <param name="rawToken">The raw token from the Authorization header.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<PersonalAccessTokenValidationResult>> Validate(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns metadata summaries for a user's non-revoked tokens — never a hash or raw token.
    /// </summary>
    /// <param name="userId">The user whose tokens to list.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<PersonalAccessTokenSummary>>> List(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a single token by id, verifying ownership.
    /// </summary>
    /// <param name="userId">The owning user.</param>
    /// <param name="tokenId">The token to revoke.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> Revoke(Guid userId, Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for a user (e.g. on password change or account lock).
    /// </summary>
    /// <param name="userId">The user whose tokens to revoke.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<IGenericResult> RevokeAll(Guid userId, CancellationToken cancellationToken = default);
}
