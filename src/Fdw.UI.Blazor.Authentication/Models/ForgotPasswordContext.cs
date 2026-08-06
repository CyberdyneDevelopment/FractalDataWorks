namespace Fdw.UI.Blazor.Authentication.Models;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Context provided to the <c>Content</c> render fragment of <c>FdwForgotPassword</c>.
/// Exposes the identifier model, state, and submit action for the consuming template.
/// </summary>
public sealed class ForgotPasswordContext
{
    /// <summary>
    /// Gets or sets the identifier (username or email) entered by the user.
    /// </summary>
    public string Identifier { get; set; } = "";

    /// <summary>
    /// Gets a value indicating whether a password reset request is in progress.
    /// </summary>
    public bool IsLoading { get; }

    /// <summary>
    /// Gets the current error message, or <c>null</c> if no error.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets a value indicating whether the request was submitted successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the redirect URL if the provider requires external password reset, or <c>null</c>.
    /// </summary>
    public string? RedirectUrl { get; }

    /// <summary>
    /// Gets the callback to invoke when the form is submitted.
    /// </summary>
    public EventCallback OnSubmit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordContext"/> class.
    /// </summary>
    /// <param name="identifier">The current identifier value.</param>
    /// <param name="isLoading">Whether a request is in progress.</param>
    /// <param name="errorMessage">The current error message.</param>
    /// <param name="isSuccess">Whether the request succeeded.</param>
    /// <param name="redirectUrl">The redirect URL for external recovery.</param>
    /// <param name="onSubmit">The submit callback.</param>
    public ForgotPasswordContext(
        string identifier,
        bool isLoading,
        string? errorMessage,
        bool isSuccess,
        string? redirectUrl,
        EventCallback onSubmit)
    {
        Identifier = identifier;
        IsLoading = isLoading;
        ErrorMessage = errorMessage;
        IsSuccess = isSuccess;
        RedirectUrl = redirectUrl;
        OnSubmit = onSubmit;
    }
}
