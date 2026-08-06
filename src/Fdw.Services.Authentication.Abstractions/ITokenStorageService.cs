namespace Fdw.Services.Authentication.Clients;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines the contract for storing and retrieving authentication tokens.
/// </summary>
public interface ITokenStorageService
{
    /// <summary>
    /// Stores the access token, refresh token, and expiration information.
    /// </summary>
    /// <param name="accessToken">The JWT access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="expiresIn">The number of seconds until the access token expires.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreTokens(string accessToken, string refreshToken, int expiresIn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stored access token, or <c>null</c> if not available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token, or <c>null</c>.</returns>
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stored refresh token, or <c>null</c> if not available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refresh token, or <c>null</c>.</returns>
    Task<string?> GetRefreshToken(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the token expiration time, or <c>null</c> if not available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The expiration time, or <c>null</c>.</returns>
    Task<DateTimeOffset?> GetExpiration(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all stored tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearTokens(CancellationToken cancellationToken = default);
}
