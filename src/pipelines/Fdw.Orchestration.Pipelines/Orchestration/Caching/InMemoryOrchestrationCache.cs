using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Fdw.Orchestration.Caching;

/// <summary>
/// In-memory implementation of the orchestration cache using IMemoryCache.
/// </summary>
/// <remarks>
/// Wraps Microsoft.Extensions.Caching.Memory.IMemoryCache for local caching.
/// Best for single-instance deployments or scenarios where cache sharing is not required.
/// </remarks>
public sealed class InMemoryOrchestrationCache : IOrchestrationCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryOrchestrationCache"/> class.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    public InMemoryOrchestrationCache(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _keys = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public Task<T?> Get<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue(key, out T? value))
        {
            return Task.FromResult(value);
        }

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc/>
    public Task Set<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var memoryCacheOptions = ConvertOptions(options);
        _cache.Set(key, value, memoryCacheOptions);
        _keys.TryAdd(key, 0);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _cache.Remove(key);
        _keys.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<T> GetOrCreate<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_cache.TryGetValue(key, out T? existing) && existing is not null)
        {
            return existing;
        }

        var value = await factory(cancellationToken).ConfigureAwait(false);
        await Set(key, value, options, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc/>
    public Task<bool> Exists(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }

    /// <inheritdoc/>
    public Task RemoveByPattern(string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Simple wildcard pattern matching
        var keysToRemove = _keys.Keys
            .Where(k => MatchesPattern(k, pattern))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private static MemoryCacheEntryOptions ConvertOptions(CacheEntryOptions? options)
    {
        var memoryCacheOptions = new MemoryCacheEntryOptions();

        if (options == null)
        {
            return memoryCacheOptions;
        }

        if (options.AbsoluteExpiration.HasValue)
        {
            memoryCacheOptions.AbsoluteExpiration = options.AbsoluteExpiration.Value;
        }

        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            memoryCacheOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow.Value;
        }

        if (options.SlidingExpiration.HasValue)
        {
            memoryCacheOptions.SlidingExpiration = options.SlidingExpiration.Value;
        }

        memoryCacheOptions.Priority = options.Priority.Name switch
        {
            "Low" => CacheItemPriority.Low,
            "Normal" => CacheItemPriority.Normal,
            "High" => CacheItemPriority.High,
            "NeverRemove" => CacheItemPriority.NeverRemove,
            _ => CacheItemPriority.Normal
        };

        return memoryCacheOptions;
    }

    private static bool MatchesPattern(string value, string pattern)
    {
        // Simple wildcard matching supporting only '*' at start and/or end
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
