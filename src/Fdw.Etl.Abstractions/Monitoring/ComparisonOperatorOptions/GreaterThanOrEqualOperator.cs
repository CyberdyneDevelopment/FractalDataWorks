using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

/// <summary>
/// Greater than or equal to operator.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComparisonOperators), "GreaterThanOrEqual", RestrictToCurrentCompilation = true)]
public sealed class GreaterThanOrEqualOperator : ComparisonOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualOperator"/> class.
    /// </summary>
    public GreaterThanOrEqualOperator() : base(3, "GreaterThanOrEqual", ">=") { }

    /// <inheritdoc />
    public override bool Evaluate(double left, double right) => left >= right;
}
