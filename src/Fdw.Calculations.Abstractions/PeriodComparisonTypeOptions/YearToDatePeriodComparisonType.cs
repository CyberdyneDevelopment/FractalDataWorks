using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Year-to-Date comparison (cumulative for current year).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "YearToDate", RestrictToCurrentCompilation = true)]
public sealed class YearToDatePeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YearToDatePeriodComparisonType"/> class.
    /// </summary>
    public YearToDatePeriodComparisonType() : base(6, "YearToDate", isCumulative: true) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => null;

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) => null;

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate) =>
        new DateTimeOffset(referenceDate.Year, 1, 1, 0, 0, 0, referenceDate.Offset);
}
