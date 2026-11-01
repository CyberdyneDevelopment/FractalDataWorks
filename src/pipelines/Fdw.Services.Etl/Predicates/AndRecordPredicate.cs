using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition met when both of its parts are.</summary>
internal sealed class AndRecordPredicate(IRecordPredicate left, IRecordPredicate right) : IRecordPredicate
{
    public bool Evaluate(IDictionary<string, object?> record) => left.Evaluate(record) && right.Evaluate(record);
}
