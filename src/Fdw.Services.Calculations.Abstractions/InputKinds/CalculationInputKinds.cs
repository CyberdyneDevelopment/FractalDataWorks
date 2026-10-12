using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// TypeCollection for calculation input kinds.
/// Source generator discovers all types decorated with [TypeOption(typeof(CalculationInputKinds), ...)] and generates All(), ById(), ByName(), and NotFound() members.
/// </summary>
[TypeCollection(typeof(CalculationInputKindBase), typeof(ICalculationInputKind), typeof(CalculationInputKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class CalculationInputKinds : TypeCollectionBase<CalculationInputKindBase, ICalculationInputKind>
{
}
