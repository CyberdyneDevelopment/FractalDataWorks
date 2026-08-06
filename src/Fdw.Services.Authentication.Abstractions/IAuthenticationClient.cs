namespace Fdw.Services.Authentication.Clients;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Defines the contract for an authentication client that manages user login, logout, and token operations.
/// </summary>
public interface IAuthenticationClient
{
    /// <summary>
    /// Gets the currently authenticated user, or <c>null</c> if not authenticated.
    /// </summary>
    UserInfo? CurrentUser { get; }

    /// <summary>
    /// Gets a value indicating whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Occurs when the authentication state changes.
    /// </summary>
    event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

    /// <summary>
    /// Authenticates a user with the specified login credentials.
    /// </summary>
    /// <param name="request">The login request containing credentials.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An <see cref="AuthResult"/> indicating success or failure.</returns>
    Task<AuthResult> Login(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user and clears authentication state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Logout(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to refresh the current access token using the stored refresh token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> if the token was refreshed successfully; otherwise, <c>false</c>.</returns>
    Task<bool> RefreshToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current access token, or <c>null</c> if not available.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The access token string, or <c>null</c>.</returns>
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to restore the authentication state from stored tokens.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> if the auth state was restored successfully; otherwise, <c>false</c>.</returns>
    Task<bool> TryRestoreAuthState(CancellationToken cancellationToken = default);
}
