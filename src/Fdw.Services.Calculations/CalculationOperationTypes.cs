using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// TypeCollection for composable calculation operations.
/// Source generator discovers all types decorated with
/// <c>[TypeOption(typeof(CalculationOperationTypes), ...)]</c> and generates
/// <c>All()</c>, <c>ById()</c>, <c>ByName()</c>, and <c>NotFound()</c> members.
/// </summary>
[TypeCollection(typeof(CalculationOperationBase), typeof(ICalculationOperation), typeof(CalculationOperationTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class CalculationOperationTypes : TypeCollectionBase<CalculationOperationBase, ICalculationOperation>
{
}
