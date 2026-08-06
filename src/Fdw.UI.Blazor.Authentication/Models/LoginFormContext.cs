namespace Fdw.UI.Blazor.Authentication.Models;

using System;
using Fdw.Services.Authentication.Clients.Models;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Context provided to the <c>FormContent</c> render fragment of <c>FdwLoginForm</c>.
/// Exposes the form model, state, and submit action for the consuming template.
/// </summary>
public sealed class LoginFormContext
{
    /// <summary>
    /// Gets the login request model bound to the form.
    /// </summary>
    public LoginRequest Model { get; }

    /// <summary>
    /// Gets a value indicating whether a login request is in progress.
    /// </summary>
    public bool IsLoading { get; }

    /// <summary>
    /// Gets the current error message, or <c>null</c> if no error.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the callback to invoke when the form is submitted.
    /// </summary>
    public EventCallback OnSubmit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginFormContext"/> class.
    /// </summary>
    /// <param name="model">The login request model.</param>
    /// <param name="isLoading">Whether a login is in progress.</param>
    /// <param name="errorMessage">The current error message.</param>
    /// <param name="onSubmit">The submit callback.</param>
    public LoginFormContext(LoginRequest model, bool isLoading, string? errorMessage, EventCallback onSubmit)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        IsLoading = isLoading;
        ErrorMessage = errorMessage;
        OnSubmit = onSubmit;
    }
}
