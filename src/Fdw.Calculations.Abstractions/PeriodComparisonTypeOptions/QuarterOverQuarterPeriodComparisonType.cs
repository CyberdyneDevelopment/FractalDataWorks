using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Quarter-over-Quarter comparison (compare to previous quarter).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "QuarterOverQuarter", RestrictToCurrentCompilation = true)]
public sealed class QuarterOverQuarterPeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuarterOverQuarterPeriodComparisonType"/> class.
    /// </summary>
    public QuarterOverQuarterPeriodComparisonType() : base(3, "QuarterOverQuarter", isCumulative: false) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => TimeSpan.FromDays(-91);

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) =>
        referenceDate.AddMonths(-3);

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate) => null;
}
