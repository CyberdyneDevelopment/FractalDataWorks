using System.Collections.Generic;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A condition on whether a field holds nothing (<c>== null</c>) or something (<c>!= null</c>).</summary>
internal sealed class NullCheckRecordPredicate(string fieldName, bool wantsNull) : IRecordPredicate
{
    public bool Evaluate(IDictionary<string, object?> record)
    {
        var holdsSomething = record.TryGetValue(fieldName, out var value) && value is not null;
        return wantsNull ? !holdsSomething : holdsSomething;
    }
}
