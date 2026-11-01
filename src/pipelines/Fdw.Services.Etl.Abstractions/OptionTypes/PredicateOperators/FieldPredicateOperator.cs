using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A field name alone is true when the field holds a true value.</summary>
[TypeOption(typeof(PredicateOperators), "Field")]
public sealed class FieldPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="FieldPredicateOperator"/> class.</summary>
    public FieldPredicateOperator() : base("Field", ["field"], "A field name alone is true when the field holds a true value.", "Field", "IsActive")
    {
    }
}
