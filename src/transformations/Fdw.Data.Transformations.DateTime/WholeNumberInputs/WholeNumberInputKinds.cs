using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>The types a date-time transformer accepts for a whole-number input.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller refuses it.</remarks>
[TypeCollection(typeof(WholeNumberInputKindBase), typeof(IWholeNumberInputKind), typeof(WholeNumberInputKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class WholeNumberInputKinds : TypeCollectionBase<WholeNumberInputKindBase, IWholeNumberInputKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IWholeNumberInputKind OfValue(object value)
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

    /// <summary>Reads a whole-number input as a long.</summary>
    /// <param name="value">The input, which is not null.</param>
    /// <returns>The whole number, or null when no type holds the input.</returns>
    public static long? Read(object value)
    {
        var kind = OfValue(value);
        return ReferenceEquals(kind, NotFound) ? null : kind.Read(value);
    }
}
