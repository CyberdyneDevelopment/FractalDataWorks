using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field equals the value; a field the record lacks is not equal.</summary>
[TypeOption(typeof(PredicateOperators), "Equal")]
public sealed class EqualPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="EqualPredicateOperator"/> class.</summary>
    public EqualPredicateOperator() : base("Equal", ["=="], "True when the field equals the value; a field the record lacks is not equal.", "Comparison", "Region == 'West'")
    {
    }
}
