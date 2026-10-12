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

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagToKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _inFlight = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of <see cref="DataGatewayResultCache"/> backed by the
    /// given <see cref="IMemoryCache"/>.
    /// </summary>
    /// <param name="cache">The memory cache used to store result entries.</param>
    /// <param name="loggerFactory">Optional logger factory; falls back to <see cref="NullLoggerFactory"/> when null.</param>
    public DataGatewayResultCache(IMemoryCache cache, ILoggerFactory? loggerFactory)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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

    /// <summary>Stores a typed result under the given key, tracking its invalidation tags.</summary>
    public void Set<T>(string cacheKey, IGenericResult<T> value, IReadOnlyList<string> tags, TimeSpan duration)
    {
        var entryOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };

        entryOptions.RegisterPostEvictionCallback((key, _, _, _) => RemoveKeyFromAllTags(key?.ToString()));

        _ = _cache.Set(cacheKey, value, entryOptions);
        TrackKeyForTags(cacheKey, tags);

        DataGatewayCacheLog.CacheMiss(_logger, cacheKey, duration);
    }

    /// <inheritdoc />
    public void InvalidateByTag(string tag)
    {
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

    private void TrackKeyForTags(string cacheKey, IReadOnlyList<string> tags)
    {
        foreach (var tag in tags)
        {
            var keySet = _tagToKeys.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
            keySet.TryAdd(cacheKey, 0);
        }
    }

    private void RemoveKeyFromAllTags(string? cacheKey)
    {
        if (cacheKey is null) return;

        foreach (var kvp in _tagToKeys)
            kvp.Value.TryRemove(cacheKey, out _);
    }
}
