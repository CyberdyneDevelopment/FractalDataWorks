using System;
using System.Collections;
using System.Collections.Generic;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Rendering.Blazor.Components;

/// <summary>
/// Non-generic access to the generic <c>Options</c>/<c>SelectedValues</c> members of
/// select-style component models.
/// </summary>
// Why: ISelectableComponentModel<T>/IMultiSelectComponentModel<T> expose options only on the
// generic interface, but a renderer dispatches on the NON-generic interfaces. Spectre solves
// this the same way (SpectreUIRenderer.GetSelectDisplayText); a contract-level fix (non-generic
// option access on the abstractions) is tracked as follow-up on FDW-546.
internal static class SelectOptionAccess
{
    /// <summary>
    /// Reads the options of a select-style model as (value, displayText, isDisabled) triples.
    /// </summary>
    internal static IReadOnlyList<(object? Value, string DisplayText, bool IsDisabled)> GetOptions(IComponentModel model)
    {
        var results = new List<(object?, string, bool)>();
        if (model.GetType().GetProperty("Options")?.GetValue(model) is IEnumerable options)
        {
            foreach (var opt in options)
            {
                var type = opt.GetType();
                var value = type.GetProperty("Value")?.GetValue(opt);
                var display = type.GetProperty("DisplayText")?.GetValue(opt)?.ToString() ?? value?.ToString() ?? string.Empty;
                var disabled = type.GetProperty("IsDisabled")?.GetValue(opt) is true;
                results.Add((value, display, disabled));
            }
        }
        return results;
    }

    /// <summary>
    /// Reads the display text for the currently selected value, falling back to the raw value.
    /// </summary>
    internal static string? GetDisplayText(IComponentModel model, object? selectedValue)
    {
        if (selectedValue is null) return null;
        foreach (var (value, display, _) in GetOptions(model))
        {
            if (Equals(value, selectedValue))
            {
                return display;
            }
        }
        return selectedValue.ToString();
    }

    /// <summary>
    /// Reads the selected values of a multi-select model.
    /// </summary>
    internal static IReadOnlyList<object?> GetSelectedValues(IMultiSelectComponentModel model)
    {
        var results = new List<object?>();
        if (model.GetType().GetProperty("SelectedValues")?.GetValue(model) is IEnumerable selected)
        {
            foreach (var value in selected)
            {
                results.Add(value);
            }
        }
        return results;
    }

    /// <summary>
    /// Writes the selected values of a multi-select model, materializing the model's
    /// element type so the generic setter accepts the list.
    /// </summary>
    internal static void SetSelectedValues(IMultiSelectComponentModel model, IEnumerable<object?> values)
    {
        var property = model.GetType().GetProperty("SelectedValues");
        if (property?.PropertyType.GenericTypeArguments is not [var elementType])
        {
            return;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (var value in values)
        {
            list.Add(value);
        }
        property.SetValue(model, list);
    }
}
