using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition on a field's text: <c>contains</c>, <c>startswith</c> or <c>endswith</c>, ignoring case. A field with no value fails it.</summary>
internal sealed class TextRecordPredicate(string fieldName, IRecordTextMatch match, string text) : IRecordPredicate
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

        return match.Matches(actual, text);
    }
}
