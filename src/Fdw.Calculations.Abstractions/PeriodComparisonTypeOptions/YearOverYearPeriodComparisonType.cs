using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Year-over-Year comparison (compare to same period last year).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "YearOverYear", RestrictToCurrentCompilation = true)]
public sealed class YearOverYearPeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YearOverYearPeriodComparisonType"/> class.
    /// </summary>
    public YearOverYearPeriodComparisonType() : base(1, "YearOverYear", isCumulative: false) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => TimeSpan.FromDays(-365);

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) =>
        referenceDate.AddYears(-1);

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate) => null;
}
