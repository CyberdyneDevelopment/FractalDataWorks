using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field is greater than or equal to the value.</summary>
[TypeOption(typeof(PredicateOperators), "GreaterOrEqual")]
public sealed class GreaterOrEqualPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterOrEqualPredicateOperator"/> class.</summary>
    public GreaterOrEqualPredicateOperator() : base("GreaterOrEqual", [">="], "True when the field is greater than or equal to the value.", "Comparison", "Amount >= 100")
    {
    }
}
