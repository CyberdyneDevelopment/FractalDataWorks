using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Quarter-to-Date comparison (cumulative for current quarter).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PeriodComparisonTypes), "QuarterToDate", RestrictToCurrentCompilation = true)]
public sealed class QuarterToDatePeriodComparisonType : PeriodComparisonTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuarterToDatePeriodComparisonType"/> class.
    /// </summary>
    public QuarterToDatePeriodComparisonType() : base(7, "QuarterToDate", isCumulative: true) { }

    /// <inheritdoc />
    public override TimeSpan? ComparisonOffset => null;

    /// <inheritdoc />
    public override DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate) => null;

    /// <inheritdoc />
    public override DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate)
    {
        var quarterStartMonth = ((referenceDate.Month - 1) / 3) * 3 + 1;
        return new DateTimeOffset(referenceDate.Year, quarterStartMonth, 1, 0, 0, 0, referenceDate.Offset);
    }
}
