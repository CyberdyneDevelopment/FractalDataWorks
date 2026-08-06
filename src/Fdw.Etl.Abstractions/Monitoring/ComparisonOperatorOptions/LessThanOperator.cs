using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

/// <summary>
/// Less than operator.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComparisonOperators), "LessThan", RestrictToCurrentCompilation = true)]
public sealed class LessThanOperator : ComparisonOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOperator"/> class.
    /// </summary>
    public LessThanOperator() : base(4, "LessThan", "<") { }

    /// <inheritdoc />
    public override bool Evaluate(double left, double right) => left < right;
}
