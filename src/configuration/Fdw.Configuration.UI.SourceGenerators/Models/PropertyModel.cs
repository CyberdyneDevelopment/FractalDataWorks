using System.Collections.Generic;

namespace Fdw.Configuration.UI.SourceGenerators.Models;

/// <summary>
/// Analyzed model of a configuration property.
/// </summary>
public sealed class PropertyModel
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string PropertyName { get; set; } = "";

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public string PropertyType { get; set; } = "";

    /// <summary>
    /// Gets or sets the display label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the help text.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the property group.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets whether the property is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether the property is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the validation rules.
    /// </summary>
    public IDictionary<string, object> ValidationRules { get; set; } = new Dictionary<string, object>(System.StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the UI component type.
    /// </summary>
    public ComponentTypeMapping ComponentType { get; set; } = ComponentTypeMapping.TextInput;

    /// <summary>
    /// Gets or sets whether this property references a TypeCollection.
    /// </summary>
    public bool IsTypeCollectionReference { get; set; }

    /// <summary>
    /// Gets or sets the TypeCollection name.
    /// </summary>
    public string? TypeCollectionName { get; set; }

    /// <summary>
    /// Gets or sets the TypeOption interface name.
    /// </summary>
    public string? TypeOptionInterfaceName { get; set; }

    /// <summary>
    /// Gets or sets whether this property is a collection.
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    /// Gets or sets the collection item type.
    /// </summary>
    public string? CollectionItemType { get; set; }
}
