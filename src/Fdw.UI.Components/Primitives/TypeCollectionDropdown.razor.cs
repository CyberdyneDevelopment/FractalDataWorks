using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Fdw.Collections;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Dropdown component for selecting a TypeCollection option.
/// </summary>
/// <typeparam name="TOption">The TypeOption type.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor UI components require synchronization context")]
public partial class TypeCollectionDropdown<TOption>
    where TOption : class, ITypeOption
{
    /// <summary>
    /// Gets or sets the available options. Pass <c>SomeTypes.All()</c> from the caller.
    /// </summary>
    [Parameter] public IEnumerable<TOption> Options { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected option ID.
    /// </summary>
    [Parameter] public int SelectedId { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the selection changes.
    /// </summary>
    [Parameter] public EventCallback<int> SelectedIdChanged { get; set; }

    /// <summary>
    /// Gets or sets the label text.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the help text displayed below the dropdown.
    /// </summary>
    [Parameter] public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text shown when no option is selected.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; } = "-- Select --";

    /// <summary>
    /// Gets or sets whether the dropdown is read-only.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    private static int GetOptionId(TOption option)
    {
        if (option.Id is int intId)
        {
            return intId;
        }

        return 0;
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (e.Value is string stringValue && int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            SelectedId = id;
            await SelectedIdChanged.InvokeAsync(id);
        }
    }
}
