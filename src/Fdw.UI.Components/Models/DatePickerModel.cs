using System;
using Fdw.Conventions;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Data model for date picker components.
/// </summary>
public sealed class DatePickerModel : IInputComponentModel<DateTime>
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
    public DateTime? Value { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public DateTime? DefaultValue { get; set; }

    /// <inheritdoc />
    object? IInputComponentModel.ValueAsObject
    {
        get => Value;
        set => Value = value as DateTime?;
    }

    /// <inheritdoc />
    object? IInputComponentModel.DefaultValueAsObject => DefaultValue;

    /// <inheritdoc />
    public Type ValueType => typeof(DateTime);

    /// <summary>
    /// Gets or sets the minimum allowed date.
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed date.
    /// </summary>
    public DateTime? MaxDate { get; set; }

    /// <summary>
    /// Gets or sets the display format.
    /// </summary>
    public string Format { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Gets or sets a value indicating whether to include time selection.
    /// </summary>
    public bool IncludeTime { get; set; }

    /// <summary>
    /// Gets or sets the time format when IncludeTime is true.
    /// </summary>
    public string TimeFormat { get; set; } = "HH:mm";

    /// <summary>
    /// Gets or sets the custom validator function.
    /// </summary>
    public Func<DateTime?, ValidationResult>? CustomValidator { get; set; }

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Validation logic — independent condition checks for required, min/max dates, custom validator
    public ValidationResult Validate()
    {
        if (IsRequired && !Value.HasValue)
        {
            return ValidationResult.Error($"{Label ?? Id} is required.");
        }

        if (Value.HasValue)
        {
            if (MinDate.HasValue && Value.Value < MinDate.Value)
            {
                return ValidationResult.Error($"{Label ?? Id} must be on or after {MinDate.Value.ToString(Format, System.Globalization.CultureInfo.CurrentCulture)}.");
            }

            if (MaxDate.HasValue && Value.Value > MaxDate.Value)
            {
                return ValidationResult.Error($"{Label ?? Id} must be on or before {MaxDate.Value.ToString(Format, System.Globalization.CultureInfo.CurrentCulture)}.");
            }
        }

        if (CustomValidator != null)
        {
            return CustomValidator(Value);
        }

        return ValidationResult.Success();
    }
}
