using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Extension methods for IDataSchema.
/// </summary>
public static class DataSchemaExtensions
{
    /// <summary>
    /// Gets field names from the schema.
    /// </summary>
    public static IReadOnlyList<string> FieldNames(this IDataSchema schema)
    {
        return schema.Fields.Select(f => f.Name).ToList();
    }

    /// <summary>
    /// Gets the ordinal position of a field by name.
    /// </summary>
    public static int GetOrdinal(this IDataSchema schema, string fieldName)
    {
        for (int i = 0; i < schema.Fields.Count; i++)
        {
            if (string.Equals(schema.Fields[i].Name, fieldName, StringComparison.Ordinal))
                return i;
        }

        throw new KeyNotFoundException($"Field '{fieldName}' not found in schema");
    }

    /// <summary>
    /// Tries to get the ordinal position of a field by name.
    /// </summary>
    public static bool TryGetOrdinal(this IDataSchema schema, string fieldName, out int ordinal)
    {
        for (int i = 0; i < schema.Fields.Count; i++)
        {
            if (string.Equals(schema.Fields[i].Name, fieldName, StringComparison.Ordinal))
            {
                ordinal = i;
                return true;
            }
        }

        ordinal = -1;
        return false;
    }

    /// <summary>
    /// Checks if a field exists in the schema.
    /// </summary>
    public static bool HasField(this IDataSchema schema, string fieldName)
    {
        return schema.Fields.Any(f => string.Equals(f.Name, fieldName, StringComparison.Ordinal));
    }
}
