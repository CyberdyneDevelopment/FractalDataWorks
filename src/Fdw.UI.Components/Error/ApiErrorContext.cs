namespace Fdw.UI.Components.Error;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Context for rendering API error display.
/// </summary>
// Why: pure DTO, no logic.
[ExcludeFromCodeCoverage]
public sealed class ApiErrorContext
{
    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the error code, if available.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets a value indicating whether the error is a network/connectivity error.
    /// </summary>
    public bool IsNetworkError { get; }

    /// <summary>
    /// Gets the callback to dismiss/clear the error.
    /// </summary>
    public Action OnDismiss { get; }

    /// <summary>
    /// Gets the callback to retry the failed operation.
    /// </summary>
    public Func<System.Threading.Tasks.Task>? OnRetry { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiErrorContext"/> class.
    /// </summary>
    public ApiErrorContext(
        string message,
        string? code,
        bool isNetworkError,
        Action onDismiss,
        Func<System.Threading.Tasks.Task>? onRetry)
    {
        Message = message;
        Code = code;
        IsNetworkError = isNetworkError;
        OnDismiss = onDismiss;
        OnRetry = onRetry;
    }
}
