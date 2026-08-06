namespace Fdw.UI.Components.Error;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Components.Models;

/// <summary>
/// Context for rendering a structured error display.
/// </summary>
// Why: pure DTO, no logic.
[ExcludeFromCodeCoverage]
public sealed class ErrorDisplayContext
{
    /// <summary>
    /// Gets the error response to display.
    /// </summary>
    public ErrorResponse Error { get; }

    /// <summary>
    /// Gets the severity level for the error display.
    /// </summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// Gets the callback to retry the failed operation, if retryable.
    /// </summary>
    public Func<Task>? OnRetry { get; }

    /// <summary>
    /// Gets the callback to dismiss the error.
    /// </summary>
    public Func<Task> OnDismiss { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDisplayContext"/> class.
    /// </summary>
    public ErrorDisplayContext(
        ErrorResponse error,
        ErrorSeverity severity,
        Func<Task>? onRetry,
        Func<Task> onDismiss)
    {
        Error = error;
        Severity = severity;
        OnRetry = onRetry;
        OnDismiss = onDismiss;
    }
}
