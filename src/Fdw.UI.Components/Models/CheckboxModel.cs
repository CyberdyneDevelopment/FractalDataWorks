using System;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Data model for checkbox components.
/// </summary>
public sealed class CheckboxModel : IInputComponentModel<bool>
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
    public bool? Value { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public bool? DefaultValue { get; set; }

    /// <inheritdoc />
    object? IInputComponentModel.ValueAsObject
    {
        get => Value;
        set => Value = value as bool?;
    }

    /// <inheritdoc />
    object? IInputComponentModel.DefaultValueAsObject => DefaultValue;

    /// <inheritdoc />
    public Type ValueType => typeof(bool);

    /// <summary>
    /// Gets or sets the text displayed when checked.
    /// </summary>
    public string? CheckedText { get; set; }

    /// <summary>
    /// Gets or sets the text displayed when unchecked.
    /// </summary>
    public string? UncheckedText { get; set; }

    /// <summary>
    /// Gets or sets the custom validator function.
    /// </summary>
    public Func<bool?, ValidationResult>? CustomValidator { get; set; }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        // IsRequired for checkbox typically means "must be checked"
        if (IsRequired && Value != true)
        {
            return ValidationResult.Error($"{Label ?? Id} must be checked.");
        }

        if (CustomValidator != null)
        {
            return CustomValidator(Value);
        }

        return ValidationResult.Success();
    }
}
