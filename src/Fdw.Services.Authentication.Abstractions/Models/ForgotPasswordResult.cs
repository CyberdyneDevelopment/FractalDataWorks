namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Represents the result of a forgot password request.
/// </summary>
public sealed class ForgotPasswordResult
{
    /// <summary>
    /// Gets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Gets the redirect URL for external identity providers, if applicable.
    /// </summary>
    public string? RedirectUrl { get; private set; }

    /// <summary>
    /// Gets the error message when the request failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private ForgotPasswordResult()
    {
    }

    /// <summary>
    /// Creates a successful result indicating the reset request was processed.
    /// </summary>
    /// <returns>A successful <see cref="ForgotPasswordResult"/>.</returns>
    public static ForgotPasswordResult Succeeded() => new() { Success = true };

    /// <summary>
    /// Creates a successful result with a redirect URL for external identity providers.
    /// </summary>
    /// <param name="url">The URL to redirect the user to.</param>
    /// <returns>A redirect <see cref="ForgotPasswordResult"/>.</returns>
    public static ForgotPasswordResult Redirect(string url) => new() { Success = true, RedirectUrl = url };

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the request failed.</param>
    /// <returns>A failed <see cref="ForgotPasswordResult"/>.</returns>
    public static ForgotPasswordResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
