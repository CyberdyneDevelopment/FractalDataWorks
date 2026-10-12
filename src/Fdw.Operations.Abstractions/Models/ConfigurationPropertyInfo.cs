using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Metadata for a single configuration property.
/// </summary>
public sealed class ConfigurationPropertyInfo
{
    /// <summary>Gets or sets the property name (for binding).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the user-friendly display name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the property type for UI rendering.</summary>
    public IConfigurationPropertyType? PropertyType { get; set; }
    /// <summary>Gets or sets whether this property is required.</summary>
    public bool IsRequired { get; set; }
    /// <summary>Gets or sets whether this property contains sensitive data.</summary>
    public bool IsSecret { get; set; }
    /// <summary>Gets or sets the default value.</summary>
    public object? DefaultValue { get; set; }
    /// <summary>Gets or sets the optional description/help text.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets placeholder text for input fields.</summary>
    public string? Placeholder { get; set; }
    /// <summary>Gets or sets the logical group for organizing properties.</summary>
    public string? Group { get; set; }
    /// <summary>Gets or sets the display order within a group.</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Gets or sets validation rules for this property.</summary>
    public IReadOnlyList<ValidationRuleInfo>? ValidationRules { get; set; }
    /// <summary>Gets or sets allowed values for enum/select properties.</summary>
    public IReadOnlyList<string>? AllowedValues { get; set; }
}
