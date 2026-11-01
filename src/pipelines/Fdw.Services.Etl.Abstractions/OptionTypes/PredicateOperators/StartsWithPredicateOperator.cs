using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field's text starts with the text.</summary>
[TypeOption(typeof(PredicateOperators), "StartsWith")]
public sealed class StartsWithPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="StartsWithPredicateOperator"/> class.</summary>
    public StartsWithPredicateOperator() : base("StartsWith", ["startswith"], "True when the field's text starts with the text.", "Text", "Name startswith 'A'")
    {
    }
}
