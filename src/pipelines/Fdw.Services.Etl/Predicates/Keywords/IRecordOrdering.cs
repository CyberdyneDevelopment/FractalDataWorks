using Fdw.Collections;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A comparison sign a record predicate states between a field and a value.</summary>
public interface IRecordOrdering : ITypeOption<int, RecordOrderingBase>
{
    /// <summary>Gets the sign a predicate states the comparison by.</summary>
    string Word { get; }

    /// <summary>Whether the comparison holds for the order of the field against the value.</summary>
    /// <param name="order">Below zero when the field is less than the value, zero when equal, above zero when greater.</param>
    /// <returns><c>true</c> when it holds.</returns>
    bool Holds(int order);
}
