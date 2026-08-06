using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions.FieldAccess;

namespace Fdw.Commands.Data.FieldAccess;

/// <summary>
/// Extracts field values from dictionary-based records (ExpandoObject, IDictionary).
/// </summary>
public sealed class DictionaryFieldExtractor : IFieldValueExtractor
{
    /// <inheritdoc />
    public object? GetValue(object? record, string fieldName)
    {
        if (record == null)
        {
            return null;
        }

        if (record is IDictionary<string, object> dict)
        {
            return dict.TryGetValue(fieldName, out var value) ? value : null;
        }

        return null;
    }
}
