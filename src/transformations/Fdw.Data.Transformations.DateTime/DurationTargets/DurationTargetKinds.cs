using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>The types of date and time a duration can be added to.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller refuses it.</remarks>
[TypeCollection(typeof(DurationTargetKindBase), typeof(IDurationTargetKind), typeof(DurationTargetKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class DurationTargetKinds : TypeCollectionBase<DurationTargetKindBase, IDurationTargetKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IDurationTargetKind OfValue(object value)
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
