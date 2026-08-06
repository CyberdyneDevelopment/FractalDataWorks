using System;
using System.Collections.Generic;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Describes a single property's UI representation.
/// </summary>
public class PropertyMetadata
{
    /// <summary>
    /// Property name (e.g., "SmtpHost").
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// C# type name (e.g., "string", "int").
    /// </summary>
    public string PropertyType { get; set; } = "";

    /// <summary>
    /// Component type ID (reference to ComponentTypes TypeCollection).
    /// </summary>
    public int ComponentTypeId { get; set; }

    /// <summary>
    /// Gets component type name for serialization.
    /// </summary>
    public string ComponentType => ComponentTypes.ById(ComponentTypeId)?.Name ?? "TextInput";

    /// <summary>
    /// Current property value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Display label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Help text / description.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Placeholder text for inputs.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Is this field required?
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Is this field read-only?
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Display order (for sorting).
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Display group (for grouping).
    /// </summary>
    public string? DisplayGroup { get; set; }

    /// <summary>
    /// Validation rules (pattern, min, max, etc.).
    /// </summary>
    public IDictionary<string, object> ValidationRules { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Additional property-level attributes.
    /// </summary>
    public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
