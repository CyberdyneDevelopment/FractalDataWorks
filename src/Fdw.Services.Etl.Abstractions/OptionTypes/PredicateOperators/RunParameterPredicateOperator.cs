using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A value that is filled in from a run parameter of the pipeline before the condition is read.</summary>
[TypeOption(typeof(PredicateOperators), "RunParameter")]
public sealed class RunParameterPredicateOperator : PredicateOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="RunParameterPredicateOperator"/> class.</summary>
    public RunParameterPredicateOperator() : base("RunParameter", ["{{name}}"], "A value that is filled in from a run parameter of the pipeline before the condition is read.", "Value", "Region == {{region}}")
    {
    }
}
