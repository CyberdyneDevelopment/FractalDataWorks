using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Data model for single-select components.
/// </summary>
/// <typeparam name="T">The type of the selectable options.</typeparam>
public sealed class SelectModel<T> : ISelectableComponentModel<T>
{
    private List<SelectOption<T>> _options = [];

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

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public T? DefaultValue { get; set; }

    /// <inheritdoc />
    object? IInputComponentModel.ValueAsObject
    {
        get => Value;
        set => Value = value is T typedValue ? typedValue : default;
    }

    /// <inheritdoc />
    object? IInputComponentModel.DefaultValueAsObject => DefaultValue;

    /// <inheritdoc />
    public Type ValueType => typeof(T);

    /// <inheritdoc />
    public int OptionsCount => _options.Count;

    /// <inheritdoc />
    public IReadOnlyList<SelectOption<T>> Options => _options.AsReadOnly();

    /// <inheritdoc />
    public Func<T, string>? DisplayConverter { get; set; }

    /// <summary>
    /// Gets or sets the custom validator function.
    /// </summary>
    public Func<T?, ValidationResult>? CustomValidator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show a "None" or empty option.
    /// </summary>
    public bool AllowEmpty { get; set; }

    /// <summary>
    /// Gets or sets the text for the empty option.
    /// </summary>
    public string EmptyOptionText { get; set; } = "(None)";

    /// <summary>
    /// Adds an option to the select.
    /// </summary>
    /// <param name="value">The option value.</param>
    /// <param name="displayText">The display text.</param>
    /// <param name="isDisabled">Whether the option is disabled.</param>
    public void AddOption(T value, string displayText, bool isDisabled = false)
    {
        _options.Add(new SelectOption<T>(value, displayText, isDisabled));
    }

    /// <summary>
    /// Adds multiple options to the select.
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

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        if (IsRequired && Value == null)
        {
            return ValidationResult.Error($"{Label ?? Id} is required.");
        }

        if (Value != null && !_options.Any(o => EqualityComparer<T>.Default.Equals(o.Value, Value)))
        {
            return ValidationResult.Error($"{Label ?? Id} has an invalid selection.");
        }

        if (CustomValidator != null)
        {
            return CustomValidator(Value);
        }

        return ValidationResult.Success();
    }
}
