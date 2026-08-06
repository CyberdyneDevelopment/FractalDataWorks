using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Base class for period comparison types used in time-series analysis.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class PeriodComparisonTypeBase : TypeOptionBase<int, PeriodComparisonTypeBase>, IPeriodComparisonType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeriodComparisonTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this period comparison type.</param>
    /// <param name="name">The name of this period comparison type.</param>
    /// <param name="isCumulative">Whether this is a cumulative comparison.</param>
    protected PeriodComparisonTypeBase(int id, string name, bool isCumulative)
        : base(id, name)
    {
        IsCumulative = isCumulative;
    }

    /// <inheritdoc />
    public bool IsCumulative { get; }

    /// <inheritdoc />
    public abstract TimeSpan? ComparisonOffset { get; }

    /// <inheritdoc />
    public abstract DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate);

    /// <inheritdoc />
    public abstract DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate);
}
