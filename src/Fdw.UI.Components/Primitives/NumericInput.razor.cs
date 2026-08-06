using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Numeric input component for numeric values.
/// </summary>
/// <typeparam name="TValue">The numeric type (int, decimal, double, etc.).</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor UI components require synchronization context")]
public partial class NumericInput<TValue>
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
    /// Gets or sets the minimum allowed value.
    /// </summary>
    [Parameter] public TValue? Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed value.
    /// </summary>
    [Parameter] public TValue? Max { get; set; }

    /// <summary>
    /// Gets or sets the step increment.
    /// </summary>
    [Parameter] public TValue? Step { get; set; }

    /// <summary>
    /// Gets or sets whether the input is read-only.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    private async Task HandleInput(ChangeEventArgs e)
    {
        if (e.Value is string stringValue && !string.IsNullOrEmpty(stringValue))
        {
            TValue? typedValue = (TValue?)Convert.ChangeType(stringValue, typeof(TValue), CultureInfo.InvariantCulture);
            Value = typedValue;
            await ValueChanged.InvokeAsync(typedValue);
        }
    }
}
