using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition on a field's text: <c>contains</c>, <c>startswith</c> or <c>endswith</c>, ignoring case. A field with no value fails it.</summary>
internal sealed class TextRecordPredicate(string fieldName, string keyword, string text) : IRecordPredicate
{
    public bool Evaluate(IDictionary<string, object?> record)
    {
        if (!record.TryGetValue(fieldName, out var value) || value is null)
        {
            return false;
        }

        var actual = value.ToString();
        if (actual is null)
        {
            return false;
        }

        return keyword switch
        {
            RecordPredicateKeywords.Contains => actual.Contains(text, StringComparison.OrdinalIgnoreCase),
            RecordPredicateKeywords.StartsWith => actual.StartsWith(text, StringComparison.OrdinalIgnoreCase),
            _ => actual.EndsWith(text, StringComparison.OrdinalIgnoreCase)
        };
    }
}
