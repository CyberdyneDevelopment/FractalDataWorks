using System;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions.Caching;

/// <summary>
/// Typed accessor over IDataCommand.Metadata for command-level caching.
/// The DataGateway caching decorator reads these keys to decide whether/how to cache a command result.
/// </summary>
// Why: QueryCommand<T> is sealed with init-only properties — we can't add ICacheable.
// Metadata already exists on IDataCommand with documented intent for "connection hints, caching."
// This static helper gives compile-time safety without changing sealed command types.
public static class CachePolicy
{
    /// <summary>Metadata key for cache duration (TimeSpan).</summary>
    public const string CacheDurationKey = "CacheDuration";

    /// <summary>Metadata key for cache key prefix override (string).</summary>
    public const string CacheKeyPrefixKey = "CacheKeyPrefix";

    /// <summary>Metadata key for invalidation tags (string[]).</summary>
    public const string CacheInvalidationTagsKey = "CacheInvalidationTags";

    /// <summary>Metadata key signaling that caching is enabled (bool).</summary>
    public const string CacheEnabledKey = "CacheEnabled";

    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Returns true if the command has opted into caching via Metadata.
    /// </summary>
    public static bool IsEnabled(IDataCommand command)
    {
        return command.Metadata.TryGetValue(CacheEnabledKey, out var val)
               && val is true;
    }

    /// <summary>
    /// Gets the cache duration from command Metadata, or the provided default.
    /// </summary>
    public static TimeSpan GetDuration(IDataCommand command, TimeSpan? defaultDuration = null)
    {
        if (command.Metadata.TryGetValue(CacheDurationKey, out var val) && val is TimeSpan ts)
            return ts;

        return defaultDuration ?? DefaultDuration;
    }
}
