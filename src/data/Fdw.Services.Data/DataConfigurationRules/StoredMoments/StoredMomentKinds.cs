using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>The kinds of value a stored row holds a date and time as.</summary>
[TypeCollection(typeof(StoredMomentKindBase), typeof(IStoredMomentKind), typeof(StoredMomentKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class StoredMomentKinds : TypeCollectionBase<StoredMomentKindBase, IStoredMomentKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IStoredMomentKind OfValue(object value)
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

    /// <summary>Reads a stored value when it is of a kind this collection holds.</summary>
    /// <param name="value">The value; null is none.</param>
    /// <returns>The value, or null when it is none, of no kind, or stands for none.</returns>
    public static DateTimeOffset? Read(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var option = OfValue(value);
        return ReferenceEquals(option, NotFound) ? null : option.Read(value);
    }
}
