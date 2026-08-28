using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
