using System;
using System.Text;
using Fdw.Collections;

namespace Fdw.Services.Data.Caching;

/// <summary>One kind of value a query can hold, and how it is written into a cache key.</summary>
public interface ICacheKeyValueKind : ITypeOption<int, CacheKeyValueKindBase>
{
    /// <summary>Tells whether a value is of this kind.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Writes a value of this kind into a cache key, with its type and an exact form.</summary>
    /// <param name="text">The key being built.</param>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    void Write(StringBuilder text, object value);
}
