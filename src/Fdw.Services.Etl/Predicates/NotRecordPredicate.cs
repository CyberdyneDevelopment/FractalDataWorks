using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition met when its part is not.</summary>
internal sealed class NotRecordPredicate(IRecordPredicate inner) : IRecordPredicate
{
    public bool Evaluate(IDictionary<string, object?> record) => !inner.Evaluate(record);
}
