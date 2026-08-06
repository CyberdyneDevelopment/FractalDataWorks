using System;

namespace Fdw.Services.Configuration;

/// <summary>
/// Simple cache entry with time-based expiry.
/// </summary>
internal sealed class CacheEntry<T>
{
    /// <summary>Gets the cached value.</summary>
    public T? Value { get; }

    /// <summary>Gets the expiry time.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets whether this entry has expired.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>Creates a new cache entry.</summary>
    public CacheEntry(T? value, TimeSpan expiry)
    {
        Value = value;
        ExpiresAt = DateTimeOffset.UtcNow + expiry;
    }
}
