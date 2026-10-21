using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the grouped condition that follows is false.</summary>
[TypeOption(typeof(PredicateOperators), "Not")]
public sealed class NotPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="NotPredicateOperator"/> class.</summary>
    public NotPredicateOperator() : base("Not", ["not", "!"], "True when the grouped condition that follows is false.", "Connective", "not (Region == 'West')")
    {
    }
}
