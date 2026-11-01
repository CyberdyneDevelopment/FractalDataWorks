using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition met when either of its parts is.</summary>
internal sealed class OrRecordPredicate(IRecordPredicate left, IRecordPredicate right) : IRecordPredicate
{
    public bool Evaluate(IDictionary<string, object?> record) => left.Evaluate(record) || right.Evaluate(record);
}
