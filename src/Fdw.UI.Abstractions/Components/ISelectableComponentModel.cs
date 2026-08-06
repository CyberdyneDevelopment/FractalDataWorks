using System;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Non-generic single-select component model base for common operations.
/// </summary>
public interface ISelectableComponentModel : IInputComponentModel
{
    /// <summary>
    /// Gets the number of available options.
    /// </summary>
    int OptionsCount { get; }
}
/// <summary>
/// Single-select component model.
/// </summary>
/// <typeparam name="T">The type of the selectable options.</typeparam>
public interface ISelectableComponentModel<T> : ISelectableComponentModel, IInputComponentModel<T>
{
    /// <summary>
    /// Gets the available options.
    /// </summary>
    System.Collections.Generic.IReadOnlyList<SelectOption<T>> Options { get; }

    /// <summary>
    /// Gets the function to convert an option to its display string.
    /// </summary>
    Func<T, string>? DisplayConverter { get; }
}