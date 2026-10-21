using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when both sides are true; binds tighter than or.</summary>
[TypeOption(typeof(PredicateOperators), "And")]
public sealed class AndPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="AndPredicateOperator"/> class.</summary>
    public AndPredicateOperator() : base("And", ["and", "&&"], "True when both sides are true; binds tighter than or.", "Connective", "Amount > 100 and Region == 'West'")
    {
    }
}
