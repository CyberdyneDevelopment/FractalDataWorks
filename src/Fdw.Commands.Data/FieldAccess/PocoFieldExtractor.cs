using System;
using System.Collections.Concurrent;
using System.Reflection;
using Fdw.Commands.Data.Abstractions.FieldAccess;

namespace Fdw.Commands.Data.FieldAccess;

/// <summary>
/// Extracts field values from POCOs using reflection with PropertyInfo caching.
/// </summary>
public sealed class PocoFieldExtractor : IFieldValueExtractor
{
    private static readonly ConcurrentDictionary<(Type Type, string FieldName), PropertyInfo?> PropertyCache = new();

    /// <inheritdoc />
    public object? GetValue(object? record, string fieldName)
    {
        if (record == null)
        {
            return null;
        }

        var type = record.GetType();
        var cacheKey = (type, fieldName);

        var property = PropertyCache.GetOrAdd(cacheKey, key =>
            key.Type.GetProperty(
                key.FieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

        return property?.GetValue(record);
    }
}
