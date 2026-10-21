using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field is greater than the value.</summary>
[TypeOption(typeof(PredicateOperators), "Greater")]
public sealed class GreaterPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterPredicateOperator"/> class.</summary>
    public GreaterPredicateOperator() : base("Greater", [">"], "True when the field is greater than the value.", "Comparison", "Amount > 100")
    {
    }
}
