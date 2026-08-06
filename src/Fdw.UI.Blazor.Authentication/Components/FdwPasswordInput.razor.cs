namespace Fdw.UI.Blazor.Authentication.Components;

using System.Threading.Tasks;
using Fdw.UI.Blazor.Authentication.Validation;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Headless password input component with show/hide toggle and optional complexity validation.
/// The consuming app provides toggle and validation rendering via render fragments.
/// </summary>
public sealed partial class FdwPasswordInput : ComponentBase
{
    /// <summary>
    /// Gets or sets the current password value.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = "";

    /// <summary>
    /// Gets or sets the callback invoked when the value changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the CSS class applied to the input element.
    /// </summary>
    [Parameter]
    public string? CssClass { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "";

    /// <summary>
    /// Gets or sets the autocomplete attribute value.
    /// </summary>
    [Parameter]
    public string Autocomplete { get; set; } = "current-password";

    /// <summary>
    /// Gets or sets a value indicating whether to show the visibility toggle.
    /// </summary>
    [Parameter]
    public bool ShowToggle { get; set; } = true;

    /// <summary>
    /// Gets or sets the render fragment for the show/hide toggle button.
    /// Receives a boolean indicating whether the password is currently visible.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? ToggleContent { get; set; }

    /// <summary>
    /// Gets or sets the password complexity rules for validation. If <c>null</c>, no validation is performed.
    /// </summary>
    [Parameter]
    public PasswordComplexityRules? Rules { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display real-time validation feedback.
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; }

    /// <summary>
    /// Gets or sets the render fragment for displaying validation results.
    /// Receives a <see cref="PasswordValidationResult"/>.
    /// </summary>
    [Parameter]
    public RenderFragment<PasswordValidationResult>? ValidationContent { get; set; }

    private bool _showPassword;
    private PasswordValidationResult? _validationResult;

    private async Task HandleInput(ChangeEventArgs e)
    {
        var newValue = e.Value?.ToString() ?? string.Empty;
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue).ConfigureAwait(false);

        if (Rules is not null)
        {
            _validationResult = PasswordComplexityValidator.Validate(newValue, Rules);
        }
    }

    private void ToggleVisibility()
    {
        _showPassword = !_showPassword;
    }
}
