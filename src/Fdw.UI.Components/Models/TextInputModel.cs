using System;
using Fdw.Conventions;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Data model for text input components.
/// </summary>
public sealed class TextInputModel : IInputComponentModel<string>
{
    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    public string? Placeholder { get; set; }

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
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <inheritdoc />
    object? IInputComponentModel.ValueAsObject
    {
        get => Value;
        set => Value = value as string;
    }

    /// <inheritdoc />
    object? IInputComponentModel.DefaultValueAsObject => DefaultValue;

    /// <inheritdoc />
    public Type ValueType => typeof(string);

    /// <summary>
    /// Gets or sets the maximum length.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the minimum length.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Gets or sets the regex pattern for validation.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a password field.
    /// </summary>
    public bool IsPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is multiline.
    /// </summary>
    public bool IsMultiline { get; set; }

    /// <summary>
    /// Gets or sets the custom validator function.
    /// </summary>
    public Func<string?, ValidationResult>? CustomValidator { get; set; }

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // Validation logic — independent checks for required, min/max length, pattern, custom validator
    public ValidationResult Validate()
    {
        if (IsRequired && string.IsNullOrWhiteSpace(Value))
        {
            return ValidationResult.Error($"{Label ?? Id} is required.");
        }

        if (MinLength.HasValue && Value?.Length < MinLength.Value)
        {
            return ValidationResult.Error($"{Label ?? Id} must be at least {MinLength.Value} characters.");
        }

        if (MaxLength.HasValue && Value?.Length > MaxLength.Value)
        {
            return ValidationResult.Error($"{Label ?? Id} must not exceed {MaxLength.Value} characters.");
        }

        if (!string.IsNullOrEmpty(Pattern) && !string.IsNullOrEmpty(Value))
        {
            try
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(Value, Pattern, System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)))
                {
                    return ValidationResult.Error($"{Label ?? Id} does not match the required pattern.");
                }
            }
            catch (ArgumentException ex)
            {
                // Why: the regex pattern itself is malformed; include the exception detail so the
                // consumer can diagnose the bad pattern without a logger being available here.
                return ValidationResult.Error($"Invalid validation pattern for {Label ?? Id}: {ex.Message}");
            }
        }

        if (CustomValidator != null)
        {
            return CustomValidator(Value);
        }

        return ValidationResult.Success();
    }
}
