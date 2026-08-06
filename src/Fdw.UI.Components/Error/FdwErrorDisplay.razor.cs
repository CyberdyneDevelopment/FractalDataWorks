namespace Fdw.UI.Components.Error;

using System;
using Microsoft.AspNetCore.Components;
using Fdw.UI.Components.Models;

/// <summary>
/// Headless error display component that maps an <see cref="ErrorResponse"/>
/// into an <see cref="ErrorDisplayContext"/> for rendering by a skin layer.
/// </summary>
public sealed partial class FdwErrorDisplay : ComponentBase
{
    /// <summary>Gets or sets the error response to display.</summary>
    [Parameter]
    public ErrorResponse? Error { get; set; }

    /// <summary>Gets or sets the callback invoked to retry the failed operation.</summary>
    [Parameter]
    public EventCallback OnRetry { get; set; }

    /// <summary>Gets or sets the callback invoked to dismiss the error.</summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the render fragment that defines the error display.</summary>
    [Parameter]
    public RenderFragment<ErrorDisplayContext>? Content { get; set; }

    private ErrorDisplayContext _context = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Error is null)
        {
            return;
        }

        _context = new ErrorDisplayContext(
            Error,
            MapSeverity(Error.Code),
            OnRetry.HasDelegate
                ? async () => await OnRetry.InvokeAsync()
                : null,
            async () =>
            {
                await OnDismiss.InvokeAsync();
            });
    }

    private static ErrorSeverity MapSeverity(string code)
    {
        if (code.StartsWith("TIMEOUT", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("SERVICE_UNAVAILABLE", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("GATEWAY", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorSeverity.Info;
        }

        if (code.StartsWith("PERMISSION", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("CONFLICT", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("FORBIDDEN", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("ACCESS", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorSeverity.Warning;
        }

        return ErrorSeverity.Error;
    }
}
