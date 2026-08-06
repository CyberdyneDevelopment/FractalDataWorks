using System;
using System.Globalization;
using System.Threading.Tasks;
using Fdw.UI.Components.Primitives.DateTimePickerOptions;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Date/time picker component for DateTime values.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor UI components require synchronization context")]
public partial class DateTimePicker
{
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    [Parameter] public DateTime? Value { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the value changes.
    /// </summary>
    [Parameter] public EventCallback<DateTime?> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the label text.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the help text displayed below the picker.
    /// </summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets whether the picker is read-only.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the picker mode (Date, Time, or DateTime).
    /// </summary>
    [Parameter] public IDateTimePickerMode? Mode { get; set; }

    /// <summary>
    /// Gets or sets the custom format string for the value.
    /// </summary>
    [Parameter] public string? Format { get; set; }

    private string GetInputType()
    {
        return Mode?.HtmlInputType ?? "datetime-local";
    }

    private string GetFormattedValue()
    {
        if (Value == null) return "";

        if (!string.IsNullOrEmpty(Format))
        {
            return Value.Value.ToString(Format, CultureInfo.InvariantCulture);
        }

        return Value.Value.ToString(Mode?.DisplayFormat ?? "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
    }

    private async Task HandleInput(ChangeEventArgs e)
    {
        if (e.Value is string stringValue && !string.IsNullOrEmpty(stringValue))
        {
            if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                Value = parsed;
                await ValueChanged.InvokeAsync(parsed);
            }
        }
    }
}

// DateTimePickerMode enum replaced by DateTimePickerModes TypeCollection
// See Fdw.UI.Components.Blazor.Primitives.DateTimePickerModes namespace
