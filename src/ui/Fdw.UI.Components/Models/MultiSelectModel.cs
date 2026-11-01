using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Conventions;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Data model for multi-select components.
/// </summary>
/// <typeparam name="T">The type of the selectable options.</typeparam>
public sealed class MultiSelectModel<T> : IMultiSelectComponentModel<T>
{
    private List<SelectOption<T>> _options = [];
    private List<T> _selectedValues = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public string? HelpText { get; set; }

    /// <inheritdoc />
    public bool IsRequired { get; set; }

    /// <inheritdoc />
    public bool IsReadOnly { get; set; }

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<T> SelectedValues
    {
        get => _selectedValues.AsReadOnly();
        set => _selectedValues = value?.ToList() ?? [];
    }

    /// <inheritdoc />
    public int SelectedCount => _selectedValues.Count;

    /// <inheritdoc />
    public int OptionsCount => _options.Count;

    /// <inheritdoc />
    public IReadOnlyList<SelectOption<T>> Options => _options.AsReadOnly();

    /// <inheritdoc />
    public int? MinSelections { get; set; }

    /// <inheritdoc />
    public int? MaxSelections { get; set; }

    /// <summary>
    /// Gets or sets a function to convert an option to its display string.
    /// </summary>
    public Func<T, string>? DisplayConverter { get; set; }

    /// <summary>
    /// Adds an option to the multi-select.
    /// </summary>
    /// <param name="value">The option value.</param>
    /// <param name="displayText">The display text.</param>
    /// <param name="isDisabled">Whether the option is disabled.</param>
    public void AddOption(T value, string displayText, bool isDisabled = false)
    {
        _options.Add(new SelectOption<T>(value, displayText, isDisabled));
    }

    /// <summary>
    /// Adds multiple options.
    /// </summary>
    /// <param name="options">The options to add.</param>
    public void AddOptions(IEnumerable<SelectOption<T>> options)
    {
        _options.AddRange(options);
    }

    /// <summary>
    /// Sets all options, replacing existing ones.
    /// </summary>
    /// <param name="options">The options to set.</param>
    public void SetOptions(IEnumerable<SelectOption<T>> options)
    {
        _options = options.ToList();
    }

    /// <summary>
    /// Selects a value.
    /// </summary>
    /// <param name="value">The value to select.</param>
    public void Select(T value)
    {
        if (!_selectedValues.Contains(value))
        {
            _selectedValues.Add(value);
        }
    }

    /// <summary>
    /// Deselects a value.
    /// </summary>
    /// <param name="value">The value to deselect.</param>
    public void Deselect(T value)
    {
        _selectedValues.Remove(value);
    }

    /// <summary>
    /// Clears all selections.
    /// </summary>
    public void ClearSelections()
    {
        _selectedValues.Clear();
    }

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Validation logic — independent checks for required, min/max selections, value validity
    public ValidationResult Validate()
    {
        var count = _selectedValues.Count;

        if (IsRequired && count == 0)
        {
            return ValidationResult.Error($"{Label ?? Id} requires at least one selection.");
        }

        if (MinSelections.HasValue && count < MinSelections.Value)
        {
            return ValidationResult.Error($"{Label ?? Id} requires at least {MinSelections.Value} selections.");
        }

        if (MaxSelections.HasValue && count > MaxSelections.Value)
        {
            return ValidationResult.Error($"{Label ?? Id} allows at most {MaxSelections.Value} selections.");
        }

        // Verify all selected values are valid options
        var optionValues = _options.Select(o => o.Value).ToHashSet();
        foreach (var selected in _selectedValues)
        {
            if (!optionValues.Contains(selected))
            {
                return ValidationResult.Error($"{Label ?? Id} contains an invalid selection.");
            }
        }

        return ValidationResult.Success();
    }
}
