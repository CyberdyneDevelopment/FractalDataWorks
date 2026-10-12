using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Fdw.Orchestration.Caching;

/// <summary>
/// Distributed implementation of the orchestration cache using IDistributedCache.
/// </summary>
/// <remarks>
/// Wraps Microsoft.Extensions.Caching.Distributed.IDistributedCache for distributed caching.
/// Supports Redis, SQL Server, NCache, and other IDistributedCache implementations.
/// Best for multi-instance deployments where cache sharing across nodes is required.
/// </remarks>
public sealed class DistributedOrchestrationCache : IOrchestrationCache
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ConcurrentDictionary<string, byte> _localKeyTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedOrchestrationCache"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache instance.</param>
    public DistributedOrchestrationCache(IDistributedCache cache)
        : this(cache, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedOrchestrationCache"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache instance.</param>
    /// <param name="serializerOptions">Optional JSON serializer options.</param>
    public DistributedOrchestrationCache(IDistributedCache cache, JsonSerializerOptions? serializerOptions)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        _localKeyTracker = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public async Task<T?> Get<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);

        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(bytes, _serializerOptions);
    }

    /// <inheritdoc/>
    public async Task Set<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _serializerOptions);
        var distributedOptions = ConvertOptions(options);

        await _cache.SetAsync(key, bytes, distributedOptions, cancellationToken).ConfigureAwait(false);
        _localKeyTracker.TryAdd(key, 0);
    }

    /// <inheritdoc/>
    public async Task Remove(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        _localKeyTracker.TryRemove(key, out _);
    }

    /// <inheritdoc/>
    public async Task<T> GetOrCreate<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await Get<T>(key, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        var value = await factory(cancellationToken).ConfigureAwait(false);
        await Set(key, value, options, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc/>
    public async Task<bool> Exists(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return bytes != null && bytes.Length > 0;
    }

    /// <inheritdoc/>
    public async Task RemoveByPattern(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based removal is limited in distributed cache scenarios.
        // Most distributed caches don't support pattern-based key scanning.
        // This implementation uses the local key tracker, which only tracks keys set by this instance.
        var keysToRemove = _localKeyTracker.Keys
            .Where(k => MatchesPattern(k, pattern))
            .ToList();

        foreach (var key in keysToRemove)
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            _localKeyTracker.TryRemove(key, out _);
        }
    }

    private static DistributedCacheEntryOptions ConvertOptions(CacheEntryOptions? options)
    {
        var distributedOptions = new DistributedCacheEntryOptions();

        if (options == null)
        {
            return distributedOptions;
        }

        if (options.AbsoluteExpiration.HasValue)
        {
            distributedOptions.AbsoluteExpiration = options.AbsoluteExpiration.Value;
        }

        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            distributedOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow.Value;
        }

        if (options.SlidingExpiration.HasValue)
        {
            distributedOptions.SlidingExpiration = options.SlidingExpiration.Value;
        }

        // Note: IDistributedCache doesn't support priority

        return distributedOptions;
    }

    private static bool MatchesPattern(string value, string pattern)
    {
        if (string.Equals(pattern, "*", StringComparison.Ordinal))
            return true;

        if (pattern.StartsWith('*') && pattern.EndsWith('*'))
        {
            var middle = pattern.Substring(1, pattern.Length - 2);
            return value.Contains(middle, StringComparison.Ordinal);
        }

        if (pattern.StartsWith('*'))
        {
            var suffix = pattern.Substring(1);
            return value.EndsWith(suffix, StringComparison.Ordinal);
        }

        if (pattern.EndsWith('*'))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return value.StartsWith(prefix, StringComparison.Ordinal);
        }

        return value.Equals(pattern, StringComparison.Ordinal);
    }
}
