namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Represents the result of an authentication operation.
/// </summary>
public sealed class AuthResult
{
    /// <summary>
    /// Gets a value indicating whether the authentication succeeded.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Gets the error message when authentication failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the authenticated user information when authentication succeeded.
    /// </summary>
    public UserInfo? User { get; private set; }

    private AuthResult()
    {
    }

    /// <summary>
    /// Creates a successful authentication result with the specified user.
    /// </summary>
    /// <param name="user">The authenticated user information.</param>
    /// <returns>A successful <see cref="AuthResult"/>.</returns>
    public static AuthResult Succeeded(UserInfo user) => new() { Success = true, User = user };

    /// <summary>
    /// Creates a failed authentication result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why authentication failed.</param>
    /// <returns>A failed <see cref="AuthResult"/>.</returns>
    public static AuthResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
