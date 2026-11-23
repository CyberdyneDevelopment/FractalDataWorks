using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>The kinds of value a filter compares as text.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller treats it as not of this kind.</remarks>
[TypeCollection(typeof(FilterTextKindBase), typeof(IFilterTextKind), typeof(FilterTextKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class FilterTextKinds : TypeCollectionBase<FilterTextKindBase, IFilterTextKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IFilterTextKind OfValue(object value)
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

    /// <summary>Writes a value as text, invariant, never null.</summary>
    /// <param name="value">The value; null is the empty text.</param>
    /// <returns>The text.</returns>
    /// <exception cref="InvalidOperationException">No kind holds the value, which the three kinds make impossible.</exception>
    public static string Of(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var kind = OfValue(value);
        return ReferenceEquals(kind, NotFound)
            ? throw new InvalidOperationException($"A value of type '{value.GetType().FullName}' has no text kind.")
            : kind.Text(value);
    }
}
