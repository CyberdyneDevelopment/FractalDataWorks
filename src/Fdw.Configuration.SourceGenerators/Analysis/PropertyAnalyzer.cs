using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Fdw.Configuration.SourceGenerators.Models;

namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Analyzes individual properties for code generation.
/// </summary>
public static class PropertyAnalyzer
{
    private const string ConfigurationOptionAttribute = "Fdw.Configuration.ConfigurationOptionAttribute";
    private const string DbTypeAttribute = "Fdw.Configuration.DbTypeAttribute";
    private const string ValuesFromAttribute = "Fdw.Configuration.ValuesFromAttribute";
    private const string ManagedConfigurationAttribute = "Fdw.Configuration.ManagedConfigurationAttribute";

    /// <summary>
    /// Analyzes a property symbol and creates a property model.
    /// </summary>
    public static PropertyModel Analyze(IPropertySymbol propertySymbol)
    {
        var model = new PropertyModel
        {
            PropertyName = propertySymbol.Name,
            PropertyType = propertySymbol.Type.ToDisplayString()
        };

        // Check nullability
        model.IsNullable = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                          IsNullableValueType(propertySymbol.Type);

        // Analyze the type
        AnalyzeType(propertySymbol.Type, model);

        // Navigation property detection: if the property's type (unwrapped from nullable)
        // has [ManagedConfiguration], it represents a parent-child relationship handled by
        // a separate child table. Such properties must not become SQL columns.
        if (model.IsComplexType && HasManagedConfigurationAttribute(propertySymbol.Type))
        {
            model.IsNavigationProperty = true;
            model.ExcludeFromDdl = true;
        }

        // Analyze attributes
        AnalyzeAttributes(propertySymbol, model);

        return model;
    }

    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    /// <summary>
    /// Walks the type symbol (unwrapping nullable reference type annotations) and its
    /// entire inheritance chain to check whether any type in the hierarchy carries the
    /// [ManagedConfiguration] attribute.
    /// </summary>
    private static bool HasManagedConfigurationAttribute(ITypeSymbol typeSymbol)
    {
        // Unwrap Nullable<T> value-type wrapper (e.g., CronScheduleConfiguration? as value type)
        var underlyingType = typeSymbol;
        if (underlyingType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            underlyingType is INamedTypeSymbol namedNullable)
        {
            underlyingType = namedNullable.TypeArguments[0];
        }

        // Walk the inheritance chain
        var current = underlyingType as INamedTypeSymbol;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var attr in current.GetAttributes())
            {
                var fullName = attr.AttributeClass?.ToDisplayString();
                if (string.Equals(fullName, ManagedConfigurationAttribute, StringComparison.Ordinal))
                    return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static void AnalyzeType(ITypeSymbol typeSymbol, PropertyModel model)
    {
        // Handle nullable value types
        if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            typeSymbol is INamedTypeSymbol namedNullable)
        {
            typeSymbol = namedNullable.TypeArguments[0];
            model.IsNullable = true;
        }

        // Check if enum
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            model.IsEnum = true;
            if (typeSymbol is INamedTypeSymbol enumType)
            {
                model.EnumUnderlyingType = enumType.EnumUnderlyingType?.ToDisplayString() ?? "int";
            }
            return;
        }

        // Check if collection
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeDef = namedType.ConstructedFrom.ToDisplayString();
            if (IsCollectionType(typeDef))
            {
                model.IsCollection = true;
                model.CollectionItemType = namedType.TypeArguments[0].ToDisplayString();
                return;
            }
        }

        // Check if array
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            // byte[] is handled specially as varbinary
            if (arrayType.ElementType.SpecialType != SpecialType.System_Byte)
            {
                model.IsCollection = true;
                model.CollectionItemType = arrayType.ElementType.ToDisplayString();
            }
            return;
        }

        // Check if complex type (not a primitive)
        if (!IsPrimitiveType(typeSymbol))
        {
            model.IsComplexType = true;
        }
    }

    private static bool IsCollectionType(string typeDef)
    {
        return typeDef.StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal) ||
               typeDef.StartsWith("System.Collections.Generic.IList<", StringComparison.Ordinal) ||
               typeDef.StartsWith("System.Collections.Generic.ICollection<", StringComparison.Ordinal) ||
               typeDef.StartsWith("System.Collections.Generic.IEnumerable<", StringComparison.Ordinal) ||
               typeDef.StartsWith("System.Collections.Generic.HashSet<", StringComparison.Ordinal) ||
               typeDef.StartsWith("System.Collections.Generic.ISet<", StringComparison.Ordinal) ||
               typeDef.StartsWith("System.Collections.Generic.Dictionary<", StringComparison.Ordinal) ||
               typeDef.StartsWith("System.Collections.Generic.IDictionary<", StringComparison.Ordinal);
    }

    private static bool IsPrimitiveType(ITypeSymbol typeSymbol)
    {
#pragma warning disable FDW018 // External Roslyn SpecialType enum — cannot convert to TypeCollection
        switch (typeSymbol.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_Char:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
        }
#pragma warning restore FDW018

        // Check for other known types
        var fullName = typeSymbol.ToDisplayString();
        return string.Equals(fullName, "System.Guid", StringComparison.Ordinal) ||
               string.Equals(fullName, "System.TimeSpan", StringComparison.Ordinal) ||
               string.Equals(fullName, "System.DateTimeOffset", StringComparison.Ordinal) ||
               string.Equals(fullName, "System.DateOnly", StringComparison.Ordinal) ||
               string.Equals(fullName, "System.TimeOnly", StringComparison.Ordinal) ||
               string.Equals(fullName, "byte[]", StringComparison.Ordinal);
    }

    // MA0051: Method length acceptable - comprehensive attribute analysis covering validation, mapping, and custom attributes
#pragma warning disable MA0051 // Method is too long
    private static void AnalyzeAttributes(IPropertySymbol propertySymbol, PropertyModel model)
#pragma warning restore MA0051
    {
        foreach (var attribute in propertySymbol.GetAttributes())
        {
            var attributeFullName = attribute.AttributeClass?.ToDisplayString();
            var attributeName = attribute.AttributeClass?.Name;

            // Check for [ConfigurationOption] attribute
            if (string.Equals(attributeFullName, ConfigurationOptionAttribute, StringComparison.Ordinal))
            {
                AnalyzeConfigurationOptionAttribute(attribute, model);
                continue;
            }

            // Check for [DbType] attribute
            if (string.Equals(attributeFullName, DbTypeAttribute, StringComparison.Ordinal))
            {
                AnalyzeDbTypeAttribute(attribute, model);
                continue;
            }

            // Check for [ValuesFrom] attribute
            if (string.Equals(attributeFullName, ValuesFromAttribute, StringComparison.Ordinal))
            {
                AnalyzeValuesFromAttribute(attribute, model);
                continue;
            }

            // Handle standard validation/mapping attributes
            switch (attributeName)
            {
                case "RequiredAttribute":
                    model.IsRequired = true;
                    break;

                case "MaxLengthAttribute":
                    if (attribute.ConstructorArguments.Length > 0 &&
                        attribute.ConstructorArguments[0].Value is int maxLen)
                    {
                        model.MaxLength = maxLen;
                    }
                    break;

                case "StringLengthAttribute":
                    if (attribute.ConstructorArguments.Length > 0 &&
                        attribute.ConstructorArguments[0].Value is int strMaxLen)
                    {
                        model.MaxLength = strMaxLen;
                    }
                    break;

                case "RangeAttribute":
                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        model.MinValue = attribute.ConstructorArguments[0].Value;
                        model.MaxValue = attribute.ConstructorArguments[1].Value;
                    }
                    break;

                case "KeyAttribute":
                    // Why: IsPrimaryKey removed from PropertyModel — PK identity now in KeyField tables.
                    model.IsRequired = true;
                    break;

                case "ColumnAttribute":
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (string.Equals(namedArg.Key, "Name", StringComparison.Ordinal))
                        {
                            model.ColumnName = namedArg.Value.Value?.ToString();
                        }
                    }
                    if (attribute.ConstructorArguments.Length > 0 &&
                        attribute.ConstructorArguments[0].Value is string colName)
                    {
                        model.ColumnName = colName;
                    }
                    break;

                case "NotMappedAttribute":
                case "JsonIgnoreAttribute":
                    model.ExcludeFromDdl = true;
                    break;

                case "PrecisionAttribute":
                    if (attribute.ConstructorArguments.Length >= 1 &&
                        attribute.ConstructorArguments[0].Value is int precision)
                    {
                        model.Precision = precision;
                        if (attribute.ConstructorArguments.Length >= 2 &&
                            attribute.ConstructorArguments[1].Value is int scale)
                        {
                            model.Scale = scale;
                        }
                    }
                    break;

                case "UniqueAttribute":
                    model.IsUnique = true;
                    break;

                case "IndexAttribute":
                    if (attribute.ConstructorArguments.Length > 0 &&
                        attribute.ConstructorArguments[0].Value is string indexName)
                    {
                        model.IndexName = indexName;
                    }
                    else
                    {
                        model.IndexName = $"IX_{model.PropertyName}";
                    }
                    break;

                case "EmailAddressAttribute":
                    model.IsEmail = true;
                    break;

                case "UrlAttribute":
                    model.IsUrl = true;
                    break;

                case "RegularExpressionAttribute":
                    if (attribute.ConstructorArguments.Length > 0 &&
                        attribute.ConstructorArguments[0].Value is string pattern)
                    {
                        model.RegexPattern = pattern;
                    }
                    break;
            }
        }
    }

    private static void AnalyzeConfigurationOptionAttribute(AttributeData attribute, PropertyModel model)
    {
        // Get the TypeCollection type from constructor argument
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is INamedTypeSymbol typeCollectionType)
        {
            model.TypeCollectionReference = new TypeCollectionReference
            {
                TypeCollectionFullName = typeCollectionType.ToDisplayString(),
                TypeCollectionName = typeCollectionType.Name,
                // Default table name to TypeCollection name
                TableName = typeCollectionType.Name,
                Schema = "cfg"
            };

            // Check for named arguments
            foreach (var namedArg in attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "ById":
                        if (namedArg.Value.Value is bool byId)
                            model.TypeCollectionReference.ById = byId;
                        break;
                    case "TableName":
                        if (namedArg.Value.Value is string tableName && !string.IsNullOrEmpty(tableName))
                            model.TypeCollectionReference.TableName = tableName;
                        break;
                    case "Schema":
                        if (namedArg.Value.Value is string schema && !string.IsNullOrEmpty(schema))
                            model.TypeCollectionReference.Schema = schema;
                        break;
                }
            }
        }
    }

    private static void AnalyzeDbTypeAttribute(AttributeData attribute, PropertyModel model)
    {
        // Get SqlType from constructor argument
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string sqlType)
        {
            model.DbTypeOverride = new DbTypeOverride
            {
                SqlType = sqlType
            };

            // Check for named arguments
            foreach (var namedArg in attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "MaxLength":
                        if (namedArg.Value.Value is int maxLength && maxLength > 0)
                            model.DbTypeOverride.MaxLength = maxLength;
                        break;
                    case "Precision":
                        if (namedArg.Value.Value is int precision && precision > 0)
                            model.DbTypeOverride.Precision = precision;
                        break;
                    case "Scale":
                        if (namedArg.Value.Value is int scale && scale >= 0)
                            model.DbTypeOverride.Scale = scale;
                        break;
                }
            }
        }
    }

    private static void AnalyzeValuesFromAttribute(AttributeData attribute, PropertyModel model)
    {
        if (attribute.ConstructorArguments.Length == 0)
            return;

        var arg = attribute.ConstructorArguments[0];

        // Type-based constructor: [ValuesFrom(typeof(ConnectionTypes))]
        if (arg.Value is INamedTypeSymbol typeCollectionType)
        {
            model.ValuesFromReference = new ValuesFromReference
            {
                TypeCollectionFullName = typeCollectionType.ToDisplayString(),
                TypeCollectionName = typeCollectionType.Name
            };
        }
        // String-based constructor: [ValuesFrom("CalculationTypes")]
        else if (arg.Value is string typeCollectionName && !string.IsNullOrEmpty(typeCollectionName))
        {
            model.ValuesFromReference = new ValuesFromReference
            {
                TypeCollectionFullName = typeCollectionName,
                TypeCollectionName = typeCollectionName
            };
        }

        if (model.ValuesFromReference == null)
            return;

        // Check for named arguments
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (string.Equals(namedArg.Key, "DisplayProperty", StringComparison.Ordinal))
            {
                if (namedArg.Value.Value is string displayProperty && !string.IsNullOrEmpty(displayProperty))
                    model.ValuesFromReference.DisplayProperty = displayProperty;
            }
        }
    }
}
