using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field is less than the value.</summary>
[TypeOption(typeof(PredicateOperators), "Less")]
public sealed class LessPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="LessPredicateOperator"/> class.</summary>
    public LessPredicateOperator() : base("Less", ["<"], "True when the field is less than the value.", "Comparison", "Amount < 100")
    {
    }
}
