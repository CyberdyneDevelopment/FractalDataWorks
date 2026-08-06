using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Fdw.Configuration.UI.SourceGenerators.Models;

namespace Fdw.Configuration.UI.SourceGenerators.Analysis;

/// <summary>
/// Analyzes individual properties for UI generation.
/// </summary>
public sealed class PropertyAnalyzer
{
    /// <summary>
    /// Analyzes a property symbol and creates a property model.
    /// </summary>
    /// <param name="propertySymbol">The property symbol to analyze.</param>
    /// <returns>A property model containing analyzed metadata.</returns>
    public static PropertyModel Analyze(IPropertySymbol propertySymbol)
    {
        var model = new PropertyModel
        {
            PropertyName = propertySymbol.Name,
            PropertyType = propertySymbol.Type.ToDisplayString()
        };

        // Analyze attributes
        AnalyzeAttributes(propertySymbol, model);

        // Map to component type
        model.ComponentType = TypeMapper.MapToComponentType(propertySymbol, model);

        // Detect TypeCollection references by TypeId naming convention
        if (model.PropertyName.EndsWith("TypeId", StringComparison.Ordinal))
        {
            model.IsTypeCollectionReference = true;
            model.TypeCollectionName = model.PropertyName.Substring(0, model.PropertyName.Length - 2) + "s"; // ConnectionTypeId -> ConnectionTypes
            model.TypeOptionInterfaceName = "I" + model.PropertyName.Substring(0, model.PropertyName.Length - 2); // IConnectionType
        }

        // Detect TypeCollection references by [ValuesFrom] attribute
        if (!model.IsTypeCollectionReference)
        {
            foreach (var attribute in propertySymbol.GetAttributes())
            {
                if (!string.Equals(attribute.AttributeClass?.Name, "ValuesFromAttribute", StringComparison.Ordinal))
                    continue;

                if (attribute.ConstructorArguments.Length == 0)
                    continue;

                var arg = attribute.ConstructorArguments[0];

                // Type-based constructor: [ValuesFrom(typeof(ConnectionTypes))]
                if (arg.Value is INamedTypeSymbol typeCollectionType)
                {
                    model.IsTypeCollectionReference = true;
                    model.TypeCollectionName = typeCollectionType.Name;
                }
                // String-based constructor: [ValuesFrom("FederationStrategies")]
                else if (arg.Value is string typeCollectionName && !string.IsNullOrEmpty(typeCollectionName))
                {
                    model.IsTypeCollectionReference = true;
                    model.TypeCollectionName = typeCollectionName;
                }

                break;
            }
        }

        // Detect collections
        if (propertySymbol.Type is INamedTypeSymbol namedType)
        {
            if (namedType.IsGenericType &&
                (namedType.ConstructedFrom.ToDisplayString().StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal) ||
                 namedType.ConstructedFrom.ToDisplayString().StartsWith("System.Collections.Generic.IEnumerable<", StringComparison.Ordinal)))
            {
                model.IsCollection = true;
                model.CollectionItemType = namedType.TypeArguments[0].ToDisplayString();
                model.ComponentType = ComponentTypeMapping.Collection;
            }
        }

        return model;
    }

    // MA0051: Method length acceptable - switch on attribute types for model population
#pragma warning disable MA0051 // Method is too long
    private static void AnalyzeAttributes(IPropertySymbol propertySymbol, PropertyModel model)
#pragma warning restore MA0051
    {
        foreach (var attribute in propertySymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.Name;

            switch (attributeName)
            {
                case "RequiredAttribute":
                    model.IsRequired = true;
                    break;

                case "MaxLengthAttribute":
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        model.ValidationRules["maxLength"] = attribute.ConstructorArguments[0].Value!;
                    }
                    break;

                case "MinLengthAttribute":
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        model.ValidationRules["minLength"] = attribute.ConstructorArguments[0].Value!;
                    }
                    break;

                case "RangeAttribute":
                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        model.ValidationRules["min"] = attribute.ConstructorArguments[0].Value!;
                        model.ValidationRules["max"] = attribute.ConstructorArguments[1].Value!;
                    }
                    break;

                case "RegularExpressionAttribute":
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        model.ValidationRules["pattern"] = attribute.ConstructorArguments[0].Value!;
                    }
                    break;

                case "DisplayAttribute":
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        switch (namedArg.Key)
                        {
                            case "Name":
                                model.Label = namedArg.Value.Value?.ToString();
                                break;
                            case "Description":
                                model.HelpText = namedArg.Value.Value?.ToString();
                                break;
                            case "Order":
                                if (namedArg.Value.Value is int order)
                                    model.Order = order;
                                break;
                            case "GroupName":
                                model.Group = namedArg.Value.Value?.ToString();
                                break;
                        }
                    }
                    break;
            }
        }

        // Default label if not specified
        if (string.IsNullOrEmpty(model.Label))
        {
            model.Label = SplitCamelCase(model.PropertyName);
        }
    }

    private static string SplitCamelCase(string input)
    {
        return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
    }
}
