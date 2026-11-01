using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition comparing a field with a value: <c>==</c>, <c>!=</c>, <c>&gt;=</c>, <c>&lt;=</c>, <c>&gt;</c> or <c>&lt;</c>. A field with no value is never greater or less than anything.</summary>
internal sealed class ComparisonRecordPredicate(string fieldName, string comparator, string text) : IRecordPredicate
{
    public bool Evaluate(IDictionary<string, object?> record)
    {
        if (comparator == RecordPredicateKeywords.Equal)
        {
            return RecordValues.TextEquals(record, fieldName, text);
        }

        if (comparator == RecordPredicateKeywords.NotEqual)
        {
            return !RecordValues.TextEquals(record, fieldName, text);
        }

        var order = RecordValues.Order(record, fieldName, text);
        if (order is null)
        {
            return false;
        }

        return comparator switch
        {
            RecordPredicateKeywords.GreaterOrEqual => order >= 0,
            RecordPredicateKeywords.LessOrEqual => order <= 0,
            RecordPredicateKeywords.Greater => order > 0,
            _ => order < 0
        };
    }
}
