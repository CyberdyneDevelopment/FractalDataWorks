using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>
/// Manages the lifecycle of Personal Access Tokens (PATs) for long-lived programmatic authentication.
/// </summary>
public interface IPersonalAccessTokenService
{
    /// <summary>
    /// Creates a new Personal Access Token for the specified user.
    /// The raw token value is returned only in this response and is never stored in plain text.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the token.</param>
    /// <param name="label">A user-assigned label to identify the token.</param>
    /// <param name="expiresAt">Optional UTC expiration date. Pass <c>null</c> for a non-expiring token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<PersonalAccessTokenCreatedResult>> CreateToken(
        Guid userId,
        string label,
        DateTime? expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a raw Personal Access Token and returns the associated user ID if valid.
    /// Updates the last-used timestamp on success.
    /// </summary>
    /// <param name="rawToken">The raw PAT value from the Authorization header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<PersonalAccessTokenValidationResult>> ValidateToken(
        string rawToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all non-revoked tokens for the specified user (summary view only — no raw values).
    /// </summary>
    /// <param name="userId">The ID of the user whose tokens to list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<PersonalAccessTokenSummary>>> ListTokens(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a single token by ID. The token must belong to the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the token.</param>
    /// <param name="tokenId">The ID of the token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult> RevokeToken(
        Guid userId,
        Guid tokenId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens for the specified user (e.g. on password change or account lock).
    /// </summary>
    /// <param name="userId">The ID of the user whose tokens to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult> RevokeAllTokens(
        Guid userId,
        CancellationToken cancellationToken = default);
}
