using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field's text contains the text.</summary>
[TypeOption(typeof(PredicateOperators), "Contains")]
public sealed class ContainsPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainsPredicateOperator"/> class.</summary>
    public ContainsPredicateOperator() : base("Contains", ["contains"], "True when the field's text contains the text.", "Text", "Name contains 'Corp'")
    {
    }
}
