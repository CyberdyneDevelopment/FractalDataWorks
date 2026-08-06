namespace Fdw.Configuration.UI.SourceGenerators.Models;

/// <summary>
/// Maps to ComponentTypes in UI.Web.Abstractions.
/// </summary>
#pragma warning disable FDW017
public enum ComponentTypeMapping
#pragma warning restore FDW017
{
    /// <summary>
    /// Single-line text input component.
    /// </summary>
    TextInput = 1,

    /// <summary>
    /// Numeric input component for numbers.
    /// </summary>
    NumericInput = 2,

    /// <summary>
    /// Boolean toggle switch component.
    /// </summary>
    Switch = 3,

    /// <summary>
    /// Date/time picker component.
    /// </summary>
    DateTimePicker = 4,

    /// <summary>
    /// Multi-line text area component.
    /// </summary>
    TextArea = 5,

    /// <summary>
    /// Dropdown selection component.
    /// </summary>
    Dropdown = 6,

    /// <summary>
    /// Collection editor component.
    /// </summary>
    Collection = 7
}
