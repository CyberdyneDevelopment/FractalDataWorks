namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Describes a configurable property on a task type, used to render the designer's config panel.
/// </summary>
public sealed class TaskPropertyInfo
{
    /// <summary>
    /// Gets or sets the property key as used in the configuration dictionary.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property type hint (e.g. "string", "int", "bool", "connection").
    /// </summary>
    public string PropertyType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this property is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets an optional description shown as a tooltip in the designer.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets an optional default value.
    /// </summary>
    public string? DefaultValue { get; set; }
}
