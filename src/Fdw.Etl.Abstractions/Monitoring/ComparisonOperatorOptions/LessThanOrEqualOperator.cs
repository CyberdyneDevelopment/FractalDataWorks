using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

/// <summary>
/// Less than or equal to operator.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComparisonOperators), "LessThanOrEqual", RestrictToCurrentCompilation = true)]
public sealed class LessThanOrEqualOperator : ComparisonOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualOperator"/> class.
    /// </summary>
    public LessThanOrEqualOperator() : base(5, "LessThanOrEqual", "<=") { }

    /// <inheritdoc />
    public override bool Evaluate(double left, double right) => left <= right;
}
