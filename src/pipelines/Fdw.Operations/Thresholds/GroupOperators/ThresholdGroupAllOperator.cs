using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>Every condition and group in it holds (And).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdGroupOperators), "Conjunction")]
public sealed class ThresholdGroupAllOperator : ThresholdGroupOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdGroupAllOperator"/> class.</summary>
    public ThresholdGroupAllOperator()
        : base("Conjunction", "All")
    {
    }

    /// <inheritdoc />
    public override LogicalOperator Logical => LogicalOperator.And;
}
