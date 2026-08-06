using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Day-over-Day comparison (compare to previous day).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "DayOverDay", RestrictToCurrentCompilation = true)]
public sealed class DayOverDayPeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DayOverDayPeriodComparisonType"/> class.
    /// </summary>
    public DayOverDayPeriodComparisonType() : base(5, "DayOverDay", isCumulative: false) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => TimeSpan.FromDays(-1);

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) =>
        referenceDate.AddDays(-1);

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate) => null;
}
