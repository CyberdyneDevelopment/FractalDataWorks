using Fdw.Collections;

namespace Fdw.UI.Components.Primitives.DateTimePickerOptions;

/// <summary>
/// Interface for DateTimePicker component modes.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IDateTimePickerMode : ITypeOption<int, DateTimePickerModeBase>
{
    /// <summary>
    /// Gets a value indicating whether this mode includes date selection.
    /// </summary>
    bool IncludesDate { get; }

    /// <summary>
    /// Gets a value indicating whether this mode includes time selection.
    /// </summary>
    bool IncludesTime { get; }

    /// <summary>
    /// Gets the input type for HTML5 input element.
    /// </summary>
    string HtmlInputType { get; }

    /// <summary>
    /// Gets the format string for displaying the value.
    /// </summary>
    string DisplayFormat { get; }
}
