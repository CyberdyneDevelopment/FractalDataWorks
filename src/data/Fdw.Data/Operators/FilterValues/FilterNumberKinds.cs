using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>The kinds of value a filter compares as a number.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller treats it as not of this kind.</remarks>
[TypeCollection(typeof(FilterNumberKindBase), typeof(IFilterNumberKind), typeof(FilterNumberKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class FilterNumberKinds : TypeCollectionBase<FilterNumberKindBase, IFilterNumberKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IFilterNumberKind OfValue(object value)
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

    /// <summary>Reads a value as a decimal when it is a number.</summary>
    /// <param name="value">The value; null is not a number.</param>
    /// <returns>The decimal, or null when the value is not a number.</returns>
    public static decimal? Read(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var kind = OfValue(value);
        return ReferenceEquals(kind, NotFound) ? null : kind.ToDecimal(value);
    }
}
