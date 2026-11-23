using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>The numeric types a numeric transformer accepts as input.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller refuses it.</remarks>
[TypeCollection(typeof(NumericInputKindBase), typeof(INumericInputKind), typeof(NumericInputKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class NumericInputKinds : TypeCollectionBase<NumericInputKindBase, INumericInputKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static INumericInputKind OfValue(object value)
    {
        foreach (var option in All())
        {
            if (option.Holds(value))
            {
                return option;
            }
        }

        return NotFound;
    }

    /// <summary>Widens a numeric input to a decimal.</summary>
    /// <param name="input">The input, which is not null.</param>
    /// <param name="transformName">The transformer's name, for the refusal.</param>
    /// <returns>The decimal.</returns>
    /// <exception cref="InvalidOperationException">The input is of no numeric type the transformers accept.</exception>
    public static decimal ToDecimal(object input, string transformName)
    {
        var kind = OfValue(input);
        return ReferenceEquals(kind, NotFound)
            ? throw new InvalidOperationException($"{transformName} does not support input type '{input.GetType().Name}'. Input must be numeric.")
            : kind.ToDecimal(input);
    }
}
