using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Month-to-Date comparison (cumulative for current month).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "MonthToDate", RestrictToCurrentCompilation = true)]
public sealed class MonthToDatePeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonthToDatePeriodComparisonType"/> class.
    /// </summary>
    public MonthToDatePeriodComparisonType() : base(8, "MonthToDate", isCumulative: true) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => null;

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) => null;

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate) =>
        new DateTimeOffset(referenceDate.Year, referenceDate.Month, 1, 0, 0, 0, referenceDate.Offset);
}
