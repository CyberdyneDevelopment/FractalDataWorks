using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>
/// TypeCollection for windowed calculation types.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for windowed calculation types.
/// Source generator creates static properties for each windowed calculation type.
/// </remarks>
[TypeCollection(typeof(WindowedCalculationTypeBase), typeof(IWindowedCalculationType), typeof(WindowedCalculationTypes))]
public sealed partial class WindowedCalculationTypes : TypeCollectionBase<WindowedCalculationTypeBase, IWindowedCalculationType>
{
}
