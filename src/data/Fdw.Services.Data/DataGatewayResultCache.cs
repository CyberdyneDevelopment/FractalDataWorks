using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Process-wide store of cached DataGateway results plus the tag→keys sidecar used for
/// tag-based invalidation. Registered as a <b>singleton</b> so cached results live across
/// requests; the scoped <see cref="DataGatewayService"/> consults it.
/// </summary>
/// <remarks>
/// This is the "cache" itself — state only. Caching is built directly into
/// <see cref="DataGatewayService"/> (P3); this class owns only the cache storage and
/// invalidation mechanics. Implements <see cref="ICacheInvalidator"/> so domain providers'
/// write paths evict matching entries from the shared cache.
/// </remarks>
public sealed class DataGatewayResultCache : ICacheInvalidator
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DataGatewayResultCache> _logger;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _tagToKeys = new(StringComparer.OrdinalIgnoreCase);

    // The cache's clock, the clock reading at which each tag was last invalidated, and the reading at which the whole
    // cache last was. A read takes the clock before it runs; its result is kept only when nothing it depends on was
    // invalidated after that, so a write that lands while the read runs is not undone by the read finishing after it.
    private readonly ConcurrentDictionary<string, long> _tagChangedAt = new(StringComparer.OrdinalIgnoreCase);
    private long _clock;
    private long _allChangedAt;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _inFlight = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of <see cref="DataGatewayResultCache"/> backed by the
    /// given <see cref="IMemoryCache"/>.
    /// </summary>
    /// <param name="cache">The memory cache used to store result entries.</param>
    /// <param name="loggerFactory">Optional logger factory; falls back to <see cref="NullLoggerFactory"/> when null.</param>
    public DataGatewayResultCache(IMemoryCache cache, ILoggerFactory? loggerFactory)
    {
        _cache = cache;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DataGatewayResultCache>();
    }

    /// <summary>Attempts to read a cached typed result for the given key.</summary>
    public bool TryGet<T>(string cacheKey, out IGenericResult<T>? value)
    {
        if (_cache.TryGetValue(cacheKey, out IGenericResult<T>? cached) && cached is not null)
        {
            DataGatewayCacheLog.CacheHit(_logger, cacheKey);
            value = cached;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Runs <paramref name="execute"/> for <paramref name="cacheKey"/>, or, when a call for the same key is
    /// already running, waits for that call and returns its result. Concurrent misses for one key are one
    /// read, not one per caller: a screen that fires ten requests at once, each reading the same
    /// configuration, used to read it ten times each time an entry expired (FDW-983).
    /// </summary>
    /// <typeparam name="T">The result type the key was built for; the key already names it.</typeparam>
    /// <param name="cacheKey">The key the read is cached under, which carries the caller's partition.</param>
    /// <param name="execute">The read. The call that leads runs it, and caches its own success.</param>
    /// <param name="cancellationToken">
    /// Passed to the read a waiter has to make for itself, and it ends a waiter's wait on the read that leads.
    /// It never cancels the read for other callers.
    /// </param>
    /// <returns>
    /// The result of the shared read. A failure is shared with the callers waiting on it and is not cached.
    /// When the leading call did not finish (it was cancelled, or its read threw) a waiter makes the read itself.
    /// </returns>
    public async Task<IGenericResult<T>> Coalesce<T>(
        string cacheKey,
        Func<Task<IGenericResult<T>>> execute,
        CancellationToken cancellationToken = default)
    {
        var mine = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var flight = _inFlight.GetOrAdd(cacheKey, mine);

        if (!ReferenceEquals(flight, mine))
        {
            DataGatewayCacheLog.InFlightJoined(_logger, cacheKey);

            // The wait on the read never throws, whatever became of it: a faulted or cancelled flight is one the
            // leader did not finish, and the read is this call's to make. It ends when THIS call is cancelled,
            // not only when the leader is done, and that never cancels the leader's read.
            _ = await Task.WhenAny(flight.Task.WaitAsync(cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return flight.Task.Status == TaskStatus.RanToCompletion
                ? (IGenericResult<T>)await flight.Task.ConfigureAwait(false)
                : await execute().ConfigureAwait(false);
        }

        // The result the leader has, if it has one: an exception or a cancellation leaves it null, which is
        // how the waiters are told to read for themselves.
        IGenericResult<T>? result = null;
        try
        {
            result = await execute().ConfigureAwait(false);
            return result;
        }
        finally
        {
            if (result is not null)
            {
                _ = mine.TrySetResult(result);
            }
            else
            {
                _ = mine.TrySetCanceled(cancellationToken);
            }

            _ = _inFlight.TryRemove(cacheKey, out _);
        }
    }

    /// <summary>
    /// The cache's clock now: what a read takes before it runs, and hands to <see cref="Set{T}(string, IGenericResult{T}, IReadOnlyList{string}, TimeSpan, long)"/>
    /// when it keeps its result, so that a write which lands while the read runs keeps that result out of the cache.
    /// </summary>
    /// <returns>A number that only grows; every invalidation moves it on.</returns>
    public long Now() => Interlocked.Read(ref _clock);

    /// <summary>
    /// Whether <paramref name="tag"/>, or the whole cache, was invalidated after <paramref name="readFrom"/>, a value
    /// <see cref="Now"/> returned.
    /// </summary>
    /// <param name="tag">The invalidation tag.</param>
    /// <param name="readFrom">The clock the read took before it ran.</param>
    /// <returns><c>true</c> when a result read from that moment may already be out of date.</returns>
    public bool ChangedSince(string tag, long readFrom)
        => Interlocked.Read(ref _allChangedAt) > readFrom
            || (_tagChangedAt.TryGetValue(tag, out var changedAt) && changedAt > readFrom);

    /// <summary>
    /// Stores a typed result that is current now, tracking its invalidation tags. A result that came from a read
    /// takes the overload with the clock the read took before it ran, so a write during the read is not lost.
    /// </summary>
    public void Set<T>(string cacheKey, IGenericResult<T> value, IReadOnlyList<string> tags, TimeSpan duration)
        => Set(cacheKey, value, tags, duration, Now());

    /// <summary>
    /// Stores a typed result read from <paramref name="readFrom"/>, tracking its invalidation tags. When any of the
    /// tags (or the whole cache) was invalidated after <paramref name="readFrom"/>, the result may be from before
    /// that write and is not kept.
    /// </summary>
    /// <param name="cacheKey">The key.</param>
    /// <param name="value">The result.</param>
    /// <param name="tags">The tags whose invalidation drops it.</param>
    /// <param name="duration">How long it is kept.</param>
    /// <param name="readFrom">What <see cref="Now"/> returned before the read that produced <paramref name="value"/> ran.</param>
    /// <returns><c>true</c> when the result was kept.</returns>
    public bool Set<T>(string cacheKey, IGenericResult<T> value, IReadOnlyList<string> tags, TimeSpan duration, long readFrom)
    {
        return Keep(cacheKey, value, tags, duration, _ => readFrom);
    }

    /// <summary>
    /// Stores a typed result built from several reads, each tag with the clock the read under it took before it ran.
    /// When any tag (or the whole cache) was invalidated after its own read began, the result is not kept.
    /// </summary>
    /// <param name="cacheKey">The key.</param>
    /// <param name="value">The result.</param>
    /// <param name="readFrom">Each tag whose invalidation drops the result, with the clock its read took.</param>
    /// <param name="duration">How long it is kept.</param>
    /// <returns><c>true</c> when the result was kept.</returns>
    public bool Set<T>(string cacheKey, IGenericResult<T> value, IReadOnlyDictionary<string, long> readFrom, TimeSpan duration)
    {
        return Keep(cacheKey, value, [.. readFrom.Keys], duration, tag => readFrom[tag]);
    }

    // Tracks, then stores, then looks again: an invalidation either finds the key tracked and removes the entry, or
    // moved a tag's clock past the read, which this sees and removes the entry itself. Whichever order the two run in,
    // the entry does not outlive the write.
    private bool Keep<T>(string cacheKey, IGenericResult<T> value, IReadOnlyList<string> tags, TimeSpan duration, Func<string, long> readFrom)
    {
        for (var i = 0; i < tags.Count; i++)
        {
            if (ChangedSince(tags[i], readFrom(tags[i])))
            {
                DataGatewayCacheLog.StaleResultNotKept(_logger, cacheKey, tags[i]);
                return false;
            }
        }

        // The entry's own identity in the tag sets: a callback for an entry that was replaced or removed takes out
        // only its own tracking, never that of the entry now under the same key.
        var identity = new object();
        TrackKeyForTags(cacheKey, tags, identity);

        var entryOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };
        entryOptions.RegisterPostEvictionCallback((_, _, _, _) => UntrackKey(cacheKey, tags, identity));
        _ = _cache.Set(cacheKey, value, entryOptions);

        for (var i = 0; i < tags.Count; i++)
        {
            if (ChangedSince(tags[i], readFrom(tags[i])))
            {
                _cache.Remove(cacheKey);
                DataGatewayCacheLog.StaleResultNotKept(_logger, cacheKey, tags[i]);
                return false;
            }
        }

        DataGatewayCacheLog.CacheMiss(_logger, cacheKey, duration);
        return true;
    }

    /// <inheritdoc />
    public void InvalidateByTag(string tag)
    {
        // The clock moves first, whether or not anything is cached under the tag: a read in flight for it must not
        // keep what it read once it finishes.
        var changedAt = Interlocked.Increment(ref _clock);
        _ = _tagChangedAt.AddOrUpdate(tag, changedAt, (_, before) => Math.Max(before, changedAt));

        if (!_tagToKeys.TryRemove(tag, out var keys))
        {
            DataGatewayCacheLog.TagInvalidated(_logger, tag, 0);
            return;
        }

        var count = 0;
        foreach (var key in keys.Keys)
        {
            _cache.Remove(key);
            count++;
        }

        DataGatewayCacheLog.TagInvalidated(_logger, tag, count);
    }

    /// <inheritdoc />
    public void InvalidateByTags(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
            InvalidateByTag(tag);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        // The epoch moves first, for the reason given on InvalidateByTag: no read in flight keeps what it read.
        var changedAt = Interlocked.Increment(ref _clock);
        InterlockedMax(ref _allChangedAt, changedAt);

        var totalCount = 0;
        foreach (var kvp in _tagToKeys)
        {
            foreach (var key in kvp.Value.Keys)
            {
                _cache.Remove(key);
                totalCount++;
            }
        }

        _tagToKeys.Clear();
        DataGatewayCacheLog.AllInvalidated(_logger, totalCount);
    }

    private static void InterlockedMax(ref long location, long value)
    {
        var current = Interlocked.Read(ref location);
        while (value > current)
        {
            var seen = Interlocked.CompareExchange(ref location, value, current);
            if (seen == current)
                return;

            current = seen;
        }
    }

    private void TrackKeyForTags(string cacheKey, IReadOnlyList<string> tags, object identity)
    {
        for (var i = 0; i < tags.Count; i++)
        {
            var keySet = _tagToKeys.GetOrAdd(tags[i], _ => new ConcurrentDictionary<string, object>(StringComparer.Ordinal));
            keySet[cacheKey] = identity;
        }
    }

    // Takes the entry out of the tag sets it was tracked in, only where it is still the entry tracked: the entry a
    // newer Set put under the same key keeps its tracking, so it is still dropped by the next invalidation.
    private void UntrackKey(string cacheKey, IReadOnlyList<string> tags, object identity)
    {
        for (var i = 0; i < tags.Count; i++)
        {
            if (_tagToKeys.TryGetValue(tags[i], out var keySet))
                _ = keySet.TryRemove(new KeyValuePair<string, object>(cacheKey, identity));
        }
    }
}
