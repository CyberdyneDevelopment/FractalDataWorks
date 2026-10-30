using System.Linq;
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
    /// <summary>Gets the names of the offered window functions, comma separated, for messages that tell a caller what it may use.</summary>
    public static string OfferedNames => string.Join(", ", All().Select(option => option.Name));
}
