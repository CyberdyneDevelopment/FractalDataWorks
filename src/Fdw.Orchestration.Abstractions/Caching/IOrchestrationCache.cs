using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// General-purpose cache for orchestration data.
/// </summary>
public interface IOrchestrationCache
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value, or null if not found.</returns>
    Task<T?> Get<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="options">Cache options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Set<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Remove(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from the cache, or creates it if not present.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the value if not cached.</param>
    /// <param name="options">Cache options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrCreate<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key exists.</returns>
    Task<bool> Exists(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all entries matching a pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match (supports * wildcard).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByPattern(string pattern, CancellationToken cancellationToken = default);
}