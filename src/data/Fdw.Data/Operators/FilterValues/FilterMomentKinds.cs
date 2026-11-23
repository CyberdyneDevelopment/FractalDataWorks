using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>The kinds of value a filter compares as a point in time.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller treats it as not of this kind.</remarks>
[TypeCollection(typeof(FilterMomentKindBase), typeof(IFilterMomentKind), typeof(FilterMomentKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class FilterMomentKinds : TypeCollectionBase<FilterMomentKindBase, IFilterMomentKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IFilterMomentKind OfValue(object value)
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

    /// <summary>Reads a value as a date and time when it is one.</summary>
    /// <param name="value">The value; null is not a date.</param>
    /// <returns>The date and time, or null when the value is not one.</returns>
    public static DateTimeOffset? Read(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var kind = OfValue(value);
        return ReferenceEquals(kind, NotFound) ? null : kind.ToMoment(value);
    }
}
