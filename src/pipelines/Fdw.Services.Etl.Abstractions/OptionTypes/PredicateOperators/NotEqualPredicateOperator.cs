using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field differs from the value.</summary>
[TypeOption(typeof(PredicateOperators), "NotEqual")]
public sealed class NotEqualPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="NotEqualPredicateOperator"/> class.</summary>
    public NotEqualPredicateOperator() : base("NotEqual", ["!="], "True when the field differs from the value.", "Comparison", "Status != 'Closed'")
    {
    }
}
