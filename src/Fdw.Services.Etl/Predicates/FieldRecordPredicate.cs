using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition that names only a field: met when the field holds a true value. A field the record lacks is not true.</summary>
internal sealed class FieldRecordPredicate(string fieldName) : IRecordPredicate
{
    public bool Evaluate(IDictionary<string, object?> record) => RecordValues.IsTrue(record, fieldName);
}
