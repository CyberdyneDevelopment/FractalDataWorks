using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Switch component for boolean values.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor UI components require synchronization context")]
public partial class Switch
{
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    [Parameter] public bool Value { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the value changes.
    /// </summary>
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the label text.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the help text displayed below the switch.
    /// </summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets whether the switch is read-only.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (e.Value is bool boolValue)
        {
            Value = boolValue;
            await ValueChanged.InvokeAsync(boolValue);
        }
    }
}
