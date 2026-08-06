using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Collections;
using NJsonSchema;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Web.Api.OpenApi;

/// <summary>
/// NSwag document processor that resolves [ValuesFrom] attributes on schema properties
/// and injects the TypeCollection values as enum constraints in the OpenAPI spec.
/// This makes Scalar render dropdowns for any property decorated with [ValuesFrom(typeof(SomeTypes))].
/// </summary>
/// <remarks>
/// Why: Wave C5 deletes IConfigurationType and ConfigurationTypes TypeCollection which previously
/// supplied the ValuesFromReferences lookup. BuildValuesFromLookup() now returns empty until
/// Wave A6 introduces a replacement source for property-level TypeCollection annotations.
/// The Process() method short-circuits on empty lookup, so no schema mutations occur.
/// </remarks>
public sealed class ValuesFromSchemaDocumentProcessor : IDocumentProcessor
{
    /// <inheritdoc />
    public void Process(DocumentProcessorContext context)
    {
        // Why: BuildValuesFromLookup returns empty after Wave C5 deletes ConfigurationTypes.
        // The processor is a no-op until Wave A6 adds ValuesFromReferences to IDataContainer.
        var referencesByTypeName = BuildValuesFromLookup();
        if (referencesByTypeName.Count == 0)
            return;

        foreach (var (schemaName, schema) in context.Document.Definitions)
        {
            if (!referencesByTypeName.TryGetValue(schemaName, out var propertyReferences))
                continue;

            foreach (var (propertyName, allowedValues) in propertyReferences)
            {
                // Why: NSwag serializes property names using the CLR name directly
                // (FastEndpoints doesn't apply camelCase by default in schema defs).
                // Try exact match first, then case-insensitive.
                if (!TryGetSchemaProperty(schema, propertyName, out var propSchema))
                    continue;

                if (propSchema.Enumeration.Count > 0)
                    continue;

                propSchema.Type = JsonObjectType.String;
                foreach (var value in allowedValues)
                {
                    propSchema.Enumeration.Add(value);
                }
            }
        }
    }

    /// <summary>
    /// Builds a lookup: schema name -> { property name -> allowed values }.
    /// </summary>
    /// <remarks>
    /// Why: Returns empty after Wave C5 deletes ConfigurationTypes. The lookup was driven by
    /// IConfigurationType.ValuesFromReferences which is now deleted. Returns empty until Wave A6.
    /// </remarks>
    private static Dictionary<string, Dictionary<string, IReadOnlyList<string>>> BuildValuesFromLookup()
    {
        // Why: ConfigurationTypes.All() deleted in Wave C5. Return empty so Process() short-circuits.
        return new Dictionary<string, Dictionary<string, IReadOnlyList<string>>>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Resolves the allowed values from a TypeCollection type via reflection.
    /// </summary>
    private static List<string>? ResolveTypeCollectionValues(Type collectionType, string? displayProperty)
    {
        var allMethod = collectionType.GetMethod("All", BindingFlags.Public | BindingFlags.Static);
        if (allMethod is null)
            return null;

        var items = allMethod.Invoke(null, null);
        if (items is null)
            return null;

        var names = new List<string>();
        foreach (var item in (System.Collections.IEnumerable)items)
        {
            if (item is ITypeOption typeOption)
            {
                if (displayProperty is not null)
                {
                    var propInfo = item.GetType().GetProperty(
                        displayProperty, BindingFlags.Public | BindingFlags.Instance);
                    var displayValue = propInfo?.GetValue(item)?.ToString();
                    names.Add(displayValue ?? typeOption.Name);
                }
                else
                {
                    names.Add(typeOption.Name);
                }
            }
        }

        return names;
    }

    private static Type? ResolveCollectionType(string fullTypeName)
    {
        var type = Type.GetType(fullTypeName);
        if (type is not null)
            return type;

        // Why: Type.GetType only searches mscorlib and the calling assembly.
        // TypeCollections live in various FDW assemblies, so we search all loaded assemblies.
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(fullTypeName))
            .FirstOrDefault(t => t is not null);
    }

    private static bool TryGetSchemaProperty(
        JsonSchema schema,
        string propertyName,
        out JsonSchema propSchema)
    {
        if (schema.Properties.TryGetValue(propertyName, out var property))
        {
            propSchema = property;
            return true;
        }

        // Why: some serializers use camelCase property names
        var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        if (schema.Properties.TryGetValue(camelCase, out property))
        {
            propSchema = property;
            return true;
        }

        propSchema = null!;
        return false;
    }
}
