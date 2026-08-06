using System;
using Fdw.Conventions;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Data model for numeric input components.
/// </summary>
/// <typeparam name="T">The numeric type (int, long, decimal, double, etc.).</typeparam>
public sealed class NumericInputModel<T> : IInputComponentModel<T>
    where T : struct, IComparable<T>
{
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

    /// <summary>
    /// Gets or sets the minimum allowed value.
    /// </summary>
    public T? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed value.
    /// </summary>
    public T? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the step increment for spinner controls.
    /// </summary>
    public T? Step { get; set; }

    /// <summary>
    /// Gets or sets the number of decimal places to display.
    /// </summary>
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// Gets or sets the format string for display.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show a spinner control.
    /// </summary>
    public bool ShowSpinner { get; set; }

    /// <summary>
    /// Gets or sets the custom validator function.
    /// </summary>
    public Func<T?, ValidationResult>? CustomValidator { get; set; }

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Validation logic — independent checks for required, min/max value, custom validator
    public ValidationResult Validate()
    {
        if (IsRequired && !Value.HasValue)
        {
            return ValidationResult.Error($"{Label ?? Id} is required.");
        }

        if (Value.HasValue)
        {
            if (MinValue.HasValue && Value.Value.CompareTo(MinValue.Value) < 0)
            {
                return ValidationResult.Error($"{Label ?? Id} must be at least {MinValue}.");
            }

            if (MaxValue.HasValue && Value.Value.CompareTo(MaxValue.Value) > 0)
            {
                return ValidationResult.Error($"{Label ?? Id} must not exceed {MaxValue}.");
            }
        }

        if (CustomValidator != null)
        {
            return CustomValidator(Value);
        }

        return ValidationResult.Success();
    }
}
