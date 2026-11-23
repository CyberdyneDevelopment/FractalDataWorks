using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>At least one condition or group in it holds (Or).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdGroupOperators), "Disjunction")]
public sealed class ThresholdGroupAnyOperator : ThresholdGroupOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdGroupAnyOperator"/> class.</summary>
    public ThresholdGroupAnyOperator()
        : base("Disjunction", "Any")
    {
    }

    /// <inheritdoc />
    public override LogicalOperator Logical => LogicalOperator.Or;
}
