using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>The types of value a time zone can be applied to.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller refuses it.</remarks>
[TypeCollection(typeof(TimeZoneSourceKindBase), typeof(ITimeZoneSourceKind), typeof(TimeZoneSourceKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class TimeZoneSourceKinds : TypeCollectionBase<TimeZoneSourceKindBase, ITimeZoneSourceKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static ITimeZoneSourceKind OfValue(object value)
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
}
