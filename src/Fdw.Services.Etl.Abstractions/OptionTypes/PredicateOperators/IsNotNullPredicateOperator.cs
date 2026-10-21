using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>True when the field holds something.</summary>
[TypeOption(typeof(PredicateOperators), "IsNotNull")]
public sealed class IsNotNullPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="IsNotNullPredicateOperator"/> class.</summary>
    public IsNotNullPredicateOperator() : base("IsNotNull", ["!= null"], "True when the field holds something.", "Null check", "Region != null")
    {
    }
}
