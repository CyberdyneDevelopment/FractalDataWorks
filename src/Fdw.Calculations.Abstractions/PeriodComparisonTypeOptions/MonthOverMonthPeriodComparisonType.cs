using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Month-over-Month comparison (compare to previous month).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "MonthOverMonth", RestrictToCurrentCompilation = true)]
public sealed class MonthOverMonthPeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonthOverMonthPeriodComparisonType"/> class.
    /// </summary>
    public MonthOverMonthPeriodComparisonType() : base(2, "MonthOverMonth", isCumulative: false) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => TimeSpan.FromDays(-30);

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) =>
        referenceDate.AddMonths(-1);

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate) => null;
}
