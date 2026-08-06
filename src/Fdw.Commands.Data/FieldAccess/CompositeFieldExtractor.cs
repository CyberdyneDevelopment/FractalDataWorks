using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions.FieldAccess;

namespace Fdw.Commands.Data.FieldAccess;

/// <summary>
/// Dispatches field extraction to appropriate extractor based on record type.
/// Tries dictionary extraction first (faster, no reflection), then falls back to POCO.
/// </summary>
public sealed class CompositeFieldExtractor : IFieldValueExtractor
{
    private readonly DictionaryFieldExtractor _dictionaryExtractor = new();
    private readonly PocoFieldExtractor _pocoExtractor = new();

    /// <inheritdoc />
    public object? GetValue(object? record, string fieldName)
    {
        if (record == null)
        {
            return null;
        }

        // Try dictionary extraction first (faster, no reflection)
        if (record is IDictionary<string, object>)
        {
            return _dictionaryExtractor.GetValue(record, fieldName);
        }

        // Fall back to POCO extraction
        return _pocoExtractor.GetValue(record, fieldName);
    }
}
