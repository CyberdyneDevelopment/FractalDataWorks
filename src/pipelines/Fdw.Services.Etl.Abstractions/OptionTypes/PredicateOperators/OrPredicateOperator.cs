using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when either side is true.</summary>
[TypeOption(typeof(PredicateOperators), "Or")]
public sealed class OrPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="OrPredicateOperator"/> class.</summary>
    public OrPredicateOperator() : base("Or", ["or", "||"], "True when either side is true.", "Connective", "Region == 'West' or Region == 'East'")
    {
    }
}
