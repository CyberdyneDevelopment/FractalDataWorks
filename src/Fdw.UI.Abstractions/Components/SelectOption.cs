using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Represents a selectable option.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
[ExcludeFromCodeCoverage]
public sealed class SelectOption<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectOption{T}"/> class.
    /// </summary>
    /// <param name="value">The option value.</param>
    /// <param name="displayText">The display text.</param>
    /// <param name="isDisabled">Whether this option is disabled.</param>
    public SelectOption(T value, string displayText, bool isDisabled = false)
    {
        Value = value;
        DisplayText = displayText;
        IsDisabled = isDisabled;
    }

    /// <summary>
    /// Gets the option value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the display text.
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// Gets a value indicating whether this option is disabled.
    /// </summary>
    public bool IsDisabled { get; }
}