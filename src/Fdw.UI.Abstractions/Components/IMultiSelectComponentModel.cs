using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Multi-select component model.
/// </summary>
/// <typeparam name="T">The type of the selectable options.</typeparam>
public interface IMultiSelectComponentModel<T> : IMultiSelectComponentModel
{
    /// <summary>
    /// Gets or sets the selected values.
    /// </summary>
    IReadOnlyList<T> SelectedValues { get; set; }

    /// <summary>
    /// Gets the available options.
    /// </summary>
    IReadOnlyList<SelectOption<T>> Options { get; }
}

/// <summary>
/// Non-generic multi-select component model base for common operations.
/// </summary>
public interface IMultiSelectComponentModel : IComponentModel
{
    /// <summary>
    /// Gets the count of currently selected values.
    /// </summary>
    int SelectedCount { get; }

    /// <summary>
    /// Gets the number of available options.
    /// </summary>
    int OptionsCount { get; }

    /// <summary>
    /// Gets the minimum number of selections required.
    /// </summary>
    int? MinSelections { get; }

    /// <summary>
    /// Gets the maximum number of selections allowed.
    /// </summary>
    int? MaxSelections { get; }
}