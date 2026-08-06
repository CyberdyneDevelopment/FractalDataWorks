using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

/// <summary>
/// Equal to operator.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComparisonOperators), "Equal", RestrictToCurrentCompilation = true)]
public sealed class EqualOperator : ComparisonOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EqualOperator"/> class.
    /// </summary>
    public EqualOperator() : base(0, "Equal", "=") { }

    /// <inheritdoc />
    public override bool Evaluate(double left, double right) => left == right;
}
