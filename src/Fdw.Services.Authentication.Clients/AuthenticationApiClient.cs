namespace Fdw.Services.Authentication.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for authentication operations including login, token refresh, logout,
/// user info, personal access tokens, agent keys, password management, and preferences.
/// </summary>
public sealed class AuthenticationApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationApiClient"/> class.
    /// </summary>
    public AuthenticationApiClient(HttpClient httpClient, ILogger<AuthenticationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Authenticates a user with credentials and returns access/refresh tokens.
    /// </summary>
    /// <param name="request">The token request containing username and password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the token response.</returns>
    public Task<IGenericResult<TokenResponse>> Login(TokenRequest request, CancellationToken ct = default)
        => Post<TokenRequest, TokenResponse>("auth/token", request, ct);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the new token response.</returns>
    public Task<IGenericResult<RefreshTokenResponse>> RefreshToken(RefreshTokenRequest request, CancellationToken ct = default)
        => Post<RefreshTokenRequest, RefreshTokenResponse>("auth/refresh", request, ct);

    /// <summary>
    /// Logs out the current user by invalidating their tokens.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the logout succeeded.</returns>
    public Task<IGenericResult> Logout(CancellationToken ct = default)
        => Post("auth/logout", ct);

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the user information.</returns>
    public Task<IGenericResult<GetMePayload>> GetMe(CancellationToken ct = default)
        => Get<GetMePayload>("users/me", ct);

    // ── Personal Access Tokens ──────────────────────────────────────────

    /// <summary>
    /// Creates a new personal access token for the current user.
    /// </summary>
    /// <param name="request">The token creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created token (raw value only available once).</returns>
    public Task<IGenericResult<CreateTokenResponse>> CreateToken(CreateTokenRequest request, CancellationToken ct = default)
        => Post<CreateTokenRequest, CreateTokenResponse>("users/me/tokens", request, ct);

    /// <summary>
    /// Gets all personal access tokens for the current user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of token summaries.</returns>
    public Task<IGenericResult<IReadOnlyList<PersonalAccessTokenSummaryPayload>>> GetTokens(CancellationToken ct = default)
        => GetList<PersonalAccessTokenSummaryPayload>("users/me/tokens", ct);

    /// <summary>
    /// Revokes a personal access token for the current user.
    /// </summary>
    /// <param name="tokenId">The ID of the token to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the revocation succeeded.</returns>
    public Task<IGenericResult> RevokeToken(Guid tokenId, CancellationToken ct = default)
        => Delete($"users/me/tokens/{tokenId}", ct);

    // ── Agent Keys ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new agent key for the current user.
    /// </summary>
    /// <param name="request">The agent key creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created agent key (raw value only available once).</returns>
    public Task<IGenericResult<CreateAgentKeyResponse>> CreateAgentKey(CreateAgentKeyRequest request, CancellationToken ct = default)
        => Post<CreateAgentKeyRequest, CreateAgentKeyResponse>("agent-keys", request, ct);

    /// <summary>
    /// Gets all agent keys for the current user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of agent key summaries.</returns>
    public Task<IGenericResult<IReadOnlyList<AgentKeySummaryPayload>>> GetAgentKeys(CancellationToken ct = default)
        => GetList<AgentKeySummaryPayload>("agent-keys", ct);

    /// <summary>
    /// Deletes an agent key for the current user.
    /// </summary>
    /// <param name="keyId">The ID of the agent key to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public Task<IGenericResult> DeleteAgentKey(Guid keyId, CancellationToken ct = default)
        => Delete($"agent-keys/{keyId}", ct);

    // ── User Preferences ────────────────────────────────────────────────

    /// <summary>
    /// Gets the current user's preferences as key/value pairs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the preferences dictionary.</returns>
    public Task<IGenericResult<IReadOnlyDictionary<string, string>>> GetPreferences(CancellationToken ct = default)
        => Get<IReadOnlyDictionary<string, string>>("users/me/preferences", ct);

    /// <summary>
    /// Updates the current user's preferences.
    /// </summary>
    /// <param name="request">The preferences update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the update succeeded.</returns>
    public Task<IGenericResult> UpdatePreferences(UpdatePreferencesRequest request, CancellationToken ct = default)
        => Patch(request: request, path: "users/me/preferences", ct: ct);

    // ── Password ────────────────────────────────────────────────────────

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    /// <param name="request">The change password request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the password change succeeded.</returns>
    public Task<IGenericResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct = default)
        => Post<ChangePasswordRequest>("users/me/password", request, ct);
}
