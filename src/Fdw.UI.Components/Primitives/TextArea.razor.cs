using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Text area component for multi-line text values.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor UI components require synchronization context")]
public partial class TextArea
{
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the value changes.
    /// </summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the label text.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the help text displayed below the text area.
    /// </summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the number of visible rows.
    /// </summary>
    [Parameter] public int Rows { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum length of the text.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = 2000;

    /// <summary>
    /// Gets or sets whether the text area is read-only.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    private async Task HandleInput(ChangeEventArgs e)
    {
        if (e.Value is string stringValue)
        {
            Value = stringValue;
            await ValueChanged.InvokeAsync(stringValue);
        }
    }
}
