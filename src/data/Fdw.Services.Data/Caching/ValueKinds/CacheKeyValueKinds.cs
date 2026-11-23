using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>The kinds of value a query can hold, each written exactly into a cache key.</summary>
[TypeCollection(typeof(CacheKeyValueKindBase), typeof(ICacheKeyValueKind), typeof(CacheKeyValueKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class CacheKeyValueKinds : TypeCollectionBase<CacheKeyValueKindBase, ICacheKeyValueKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static ICacheKeyValueKind OfValue(object value)
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

    /// <summary>Writes a value into a cache key by the kind it is.</summary>
    /// <param name="text">The key being built.</param>
    /// <param name="value">The value; null is written as n.</param>
    /// <exception cref="InvalidOperationException">The value is of a type no kind holds, so a query holding it has no cache key.</exception>
    public static void Append(StringBuilder text, object? value)
    {
        if (value is null)
        {
            text.Append('n');
            return;
        }

        var kind = OfValue(value);
        if (ReferenceEquals(kind, NotFound))
        {
            throw new InvalidOperationException(
                $"A value of type '{value.GetType().FullName}' has no exact text form, so a query holding it has no cache key.");
        }

        kind.Write(text, value);
    }
}
