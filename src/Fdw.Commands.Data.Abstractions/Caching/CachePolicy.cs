using System;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions.Caching;

/// <summary>
/// Typed accessor over IDataCommand.Metadata for command-level caching.
/// The DataGateway caching decorator reads these keys to decide whether/how to cache a command result.
/// </summary>
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

    /// <summary>
    /// How long a cached command result is kept when the command requests no duration of its own.
    /// A deployment sets this before the host starts.
    /// </summary>
    public static TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Returns true if the command has opted into caching via Metadata.
    /// </summary>
    public static bool IsEnabled(IDataCommand command)
    {
        return command.Metadata.TryGetValue(CacheEnabledKey, out var val)
               && val is true;
    }

    /// <summary>
    /// Gets the cache duration from command Metadata, or <see cref="DefaultDuration"/> when the
    /// command requests none.
    /// </summary>
    public static TimeSpan GetDuration(IDataCommand command)
    {
        if (command.Metadata.TryGetValue(CacheDurationKey, out var val) && val is TimeSpan ts)
            return ts;

        return DefaultDuration;
    }

    /// <summary>
    /// Gets the cache duration from command Metadata bounded by a ceiling the storage layer imposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A command states how long it would like its result kept; the connection kind states how long a
    /// result read under the caller's session may safely be replayed. This composes the two, and the
    /// composition is a minimum — a ceiling caps a request and never extends it, so a command asking
    /// for less than the ceiling keeps its shorter duration.
    /// </para>
    /// <para>
    /// <see cref="TimeSpan.MaxValue"/> is the identity for that minimum, which is what a kind imposing
    /// no bound returns. It needs no special case here: it simply never wins the comparison.
    /// </para>
    /// </remarks>
    /// <param name="command">The command whose Metadata may request a duration.</param>
    /// <param name="ceiling">The longest the storage layer permits this result to be replayed.</param>
    public static TimeSpan GetDuration(IDataCommand command, TimeSpan ceiling)
    {
        var requested = GetDuration(command);
        return requested < ceiling ? requested : ceiling;
    }
}
