using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// TypeCollection of the operators a calculation tree can use. Every option states its shape and emits itself as an
/// expression; the compiler looks an operator up by name and asks it.
/// </summary>
[TypeCollection(typeof(CalculationNodeBase), typeof(ICalculationNodeType), typeof(CalculationNodeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class CalculationNodeTypes : TypeCollectionBase<CalculationNodeBase, ICalculationNodeType>
{
}
