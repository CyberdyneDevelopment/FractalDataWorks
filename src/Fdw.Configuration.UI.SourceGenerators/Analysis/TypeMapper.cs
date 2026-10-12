using System;
using Microsoft.CodeAnalysis;
using Fdw.Configuration.UI.SourceGenerators.Models;

namespace Fdw.Configuration.UI.SourceGenerators.Analysis;

/// <summary>
/// Maps C# types to UI component types.
/// </summary>
public sealed class TypeMapper
{
    /// <summary>
    /// Maps a property symbol to its appropriate UI component type.
    /// </summary>
    /// <param name="propertySymbol">The property symbol to analyze.</param>
    /// <param name="model">The property model containing metadata.</param>
    /// <returns>The appropriate component type mapping.</returns>
    public static ComponentTypeMapping MapToComponentType(IPropertySymbol propertySymbol, PropertyModel model)
    {
        var typeName = propertySymbol.Type.ToDisplayString();

        // Check for long text (TextArea)
        if (string.Equals(typeName, "string", StringComparison.Ordinal))
        {
            if (model.ValidationRules.TryGetValue("maxLength", out var maxLength) &&
                maxLength is int maxLengthValue && maxLengthValue > 100)
            {
                return ComponentTypeMapping.TextArea;
            }
            return ComponentTypeMapping.TextInput;
        }

        // Numeric types
        if (typeName is "int" or "long" or "short" or "byte" or
            "decimal" or "double" or "float" or
            "System.Int32" or "System.Int64" or "System.Decimal" or "System.Double")
        {
            return ComponentTypeMapping.NumericInput;
        }

        // Boolean
        if (typeName is "bool" or "System.Boolean")
        {
            return ComponentTypeMapping.Switch;
        }

        // DateTime
        if (typeName is "System.DateTime" or "System.DateTimeOffset")
        {
            return ComponentTypeMapping.DateTimePicker;
        }

        // TypeCollection reference
        if (model.IsTypeCollectionReference)
        {
            return ComponentTypeMapping.Dropdown;
        }

        // Collection
        if (model.IsCollection)
        {
            return ComponentTypeMapping.Collection;
        }

        // Default to text input
        return ComponentTypeMapping.TextInput;
    }
}
