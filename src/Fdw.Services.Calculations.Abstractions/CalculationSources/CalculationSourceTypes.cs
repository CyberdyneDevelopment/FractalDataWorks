using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationSources;

/// <summary>
/// Extensible registry of calculation catalog origins. Built-in options are "Default" (codified,
/// ships with code) and "Configuration" (calc.CalculationEntity rows). A third-party package can add
/// its own source by decorating a <see cref="CalculationSourceTypeBase"/> with
/// <c>[TypeOption(typeof(CalculationSourceTypes), "Vendor")]</c> in its own assembly.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(CalculationSourceTypeBase), typeof(ICalculationSourceType), typeof(CalculationSourceTypes))]
public abstract partial class CalculationSourceTypes : TypeCollectionBase<CalculationSourceTypeBase, ICalculationSourceType>
{
}
