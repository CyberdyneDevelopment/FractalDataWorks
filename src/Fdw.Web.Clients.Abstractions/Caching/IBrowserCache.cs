namespace Fdw.Web.Clients.Abstractions.Caching;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstraction for browser-side caching of API responses.
/// In WASM mode, this is backed by IndexedDB; in Server mode,
/// a no-op <c>NullBrowserCache</c> implementation is used.
/// </summary>
public interface IBrowserCache
{
    /// <summary>
    /// Retrieves a cached response by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached data.</typeparam>
    /// <param name="cacheKey">The cache key identifying the response.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The cached response, or <c>null</c> if no entry exists for the key.</returns>
    Task<CachedResponse<T>?> Get<T>(string cacheKey, CancellationToken ct);

    /// <summary>
    /// Stores a response in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the data to cache.</typeparam>
    /// <param name="cacheKey">The cache key identifying the response.</param>
    /// <param name="data">The data to cache.</param>
    /// <param name="etag">The ETag associated with the response, if any.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the cache write operation.</returns>
    Task Set<T>(string cacheKey, T data, string? etag, CancellationToken ct);

    /// <summary>
    /// Removes a cached response by key.
    /// </summary>
    /// <param name="cacheKey">The cache key to invalidate.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the invalidation operation.</returns>
    Task Invalidate(string cacheKey, CancellationToken ct);

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the clear operation.</returns>
    Task Clear(CancellationToken ct);
}
