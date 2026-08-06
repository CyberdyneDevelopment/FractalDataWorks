using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Week-over-Week comparison (compare to previous week).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "WeekOverWeek", RestrictToCurrentCompilation = true)]
public sealed class WeekOverWeekPeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeekOverWeekPeriodComparisonType"/> class.
    /// </summary>
    public WeekOverWeekPeriodComparisonType() : base(4, "WeekOverWeek", isCumulative: false) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => TimeSpan.FromDays(-7);

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) =>
        referenceDate.AddDays(-7);

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate) => null;
}
