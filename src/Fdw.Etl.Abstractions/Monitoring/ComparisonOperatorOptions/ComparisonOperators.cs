using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

/// <summary>
/// TypeCollection for comparison operators used in alert rules.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for comparison operators.
/// Source generator creates static properties for each registered comparison operator.
/// </remarks>
[TypeCollection(typeof(ComparisonOperatorBase), typeof(IComparisonOperator), typeof(ComparisonOperators))]
public sealed partial class ComparisonOperators : TypeCollectionBase<ComparisonOperatorBase, IComparisonOperator>
{
}
