using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field is less than or equal to the value.</summary>
[TypeOption(typeof(PredicateOperators), "LessOrEqual")]
public sealed class LessOrEqualPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="LessOrEqualPredicateOperator"/> class.</summary>
    public LessOrEqualPredicateOperator() : base("LessOrEqual", ["<="], "True when the field is less than or equal to the value.", "Comparison", "Amount <= 100")
    {
    }
}
