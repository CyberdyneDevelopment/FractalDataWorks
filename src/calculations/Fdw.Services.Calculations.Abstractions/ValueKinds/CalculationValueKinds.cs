using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// TypeCollection of the kinds of value a calculation result can hold: a number, or a date-time.
/// </summary>
[TypeCollection(typeof(CalculationValueKindBase), typeof(ICalculationValueKind), typeof(CalculationValueKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class CalculationValueKinds : TypeCollectionBase<CalculationValueKindBase, ICalculationValueKind>
{
    /// <summary>Finds the kind whose values the compiled code computes as a type.</summary>
    /// <param name="type">The type of the compiled result slot.</param>
    /// <returns>The kind, or <c>NotFound</c> when no kind computes values as that type.</returns>
    public static ICalculationValueKind OfClrType(Type type)
    {
        foreach (var kind in All())
        {
            if (kind.ClrType == type)
            {
                return kind;
            }
        }

        return NotFound;
    }
}
