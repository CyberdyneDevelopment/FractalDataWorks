using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.CalculationTypeOptions;

/// <summary>
/// TypeCollection for calculation types.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for calculation types.
/// Source generator creates static properties for each calculation type.
/// Source generator will create the collection in the same namespace.
/// </remarks>
[TypeCollection(typeof(CalculationTypeBase), typeof(ICalculationType), typeof(CalculationTypes))]
public sealed partial class CalculationTypes : TypeCollectionBase<CalculationTypeBase, ICalculationType>
{
}
