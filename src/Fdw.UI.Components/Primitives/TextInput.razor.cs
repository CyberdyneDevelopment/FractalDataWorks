using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Text input component for single-line text values.
/// </summary>
/// <typeparam name="TValue">The type of value being edited.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor UI components require synchronization context")]
public partial class TextInput<TValue>
{
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the value changes.
    /// </summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the label text.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the help text displayed below the input.
    /// </summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the input.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = 255;

    /// <summary>
    /// Gets or sets whether the input is read-only.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the input is required.
    /// </summary>
    [Parameter] public bool Required { get; set; }

    private async Task HandleInput(ChangeEventArgs e)
    {
        if (e.Value is string stringValue)
        {
            TValue? typedValue = (TValue?)Convert.ChangeType(stringValue, typeof(TValue), CultureInfo.InvariantCulture);
            Value = typedValue;
            await ValueChanged.InvokeAsync(typedValue);
        }
    }
}
