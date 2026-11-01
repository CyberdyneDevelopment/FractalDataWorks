using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field's text ends with the text.</summary>
[TypeOption(typeof(PredicateOperators), "EndsWith")]
public sealed class EndsWithPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="EndsWithPredicateOperator"/> class.</summary>
    public EndsWithPredicateOperator() : base("EndsWith", ["endswith"], "True when the field's text ends with the text.", "Text", "Name endswith 'Inc'")
    {
    }
}
