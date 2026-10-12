using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// TypeCollection for period comparison types used in time-series analysis.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for period comparison types.
/// Source generator creates static properties for each registered period comparison type.
/// </remarks>
[TypeCollection(typeof(PeriodComparisonTypeBase), typeof(IPeriodComparisonType), typeof(PeriodComparisonTypes))]
public sealed partial class PeriodComparisonTypes : TypeCollectionBase<PeriodComparisonTypeBase, IPeriodComparisonType>
{
}
