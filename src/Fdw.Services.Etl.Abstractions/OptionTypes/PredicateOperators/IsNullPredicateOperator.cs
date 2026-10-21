using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field holds nothing.</summary>
[TypeOption(typeof(PredicateOperators), "IsNull")]
public sealed class IsNullPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="IsNullPredicateOperator"/> class.</summary>
    public IsNullPredicateOperator() : base("IsNull", ["== null"], "True when the field holds nothing.", "Null check", "Region == null")
    {
    }
}
