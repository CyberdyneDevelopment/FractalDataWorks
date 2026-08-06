namespace Fdw.Web.Clients.Wasm.Caching;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Clients.Abstractions.Caching;
using Fdw.Web.Clients.Wasm.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

/// <summary>
/// Browser-side cache implementation backed by IndexedDB via JS interop.
/// Provides persistent caching of API responses in Blazor WebAssembly mode
/// with LRU eviction when the cache exceeds the maximum entry count.
/// </summary>
public sealed class IndexedDbBrowserCache : IBrowserCache
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<IndexedDbBrowserCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedDbBrowserCache"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime for interop calls.</param>
    /// <param name="logger">The logger instance.</param>
    public IndexedDbBrowserCache(
        IJSRuntime jsRuntime,
        ILogger<IndexedDbBrowserCache>? logger = null)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? NullLogger<IndexedDbBrowserCache>.Instance;
    }

    /// <inheritdoc />
    public async Task<CachedResponse<T>?> Get<T>(string cacheKey, CancellationToken ct)
    {
        try
        {
            BrowserCacheLog.CacheGetAttempt(_logger, cacheKey);

            var json = await _jsRuntime.InvokeAsync<string?>(
                "fdwCache.get",
                ct,
                cacheKey).ConfigureAwait(false);

            if (json is null)
            {
                BrowserCacheLog.CacheMiss(_logger, cacheKey);
                return null;
            }

            var entry = JsonSerializer.Deserialize<CacheEntry>(json);
            if (entry is null)
            {
                BrowserCacheLog.CacheMiss(_logger, cacheKey);
                return null;
            }

            var data = JsonSerializer.Deserialize<T>(entry.Data);
            if (data is null)
            {
                BrowserCacheLog.CacheDeserializationFailed(_logger, cacheKey);
                return null;
            }

            BrowserCacheLog.CacheHit(_logger, cacheKey);
            return new CachedResponse<T>(data, entry.ETag, entry.Timestamp);
        }
        catch (JSException ex)
        {
            BrowserCacheLog.CacheOperationFailed(_logger, ex, "Get", cacheKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task Set<T>(string cacheKey, T data, string? etag, CancellationToken ct)
    {
        try
        {
            BrowserCacheLog.CacheSetAttempt(_logger, cacheKey);

            var dataJson = JsonSerializer.Serialize(data);
            var entry = new CacheEntry
            {
                Key = cacheKey,
                Data = dataJson,
                ETag = etag,
                Timestamp = DateTimeOffset.UtcNow
            };

            var entryJson = JsonSerializer.Serialize(entry);

            await _jsRuntime.InvokeVoidAsync(
                "fdwCache.set",
                ct,
                cacheKey,
                entryJson).ConfigureAwait(false);

            BrowserCacheLog.CacheSetCompleted(_logger, cacheKey);
        }
        catch (JSException ex)
        {
            BrowserCacheLog.CacheOperationFailed(_logger, ex, "Set", cacheKey);
        }
    }

    /// <inheritdoc />
    public async Task Invalidate(string cacheKey, CancellationToken ct)
    {
        try
        {
            BrowserCacheLog.CacheInvalidateAttempt(_logger, cacheKey);

            await _jsRuntime.InvokeVoidAsync(
                "fdwCache.invalidate",
                ct,
                cacheKey).ConfigureAwait(false);

            BrowserCacheLog.CacheInvalidateCompleted(_logger, cacheKey);
        }
        catch (JSException ex)
        {
            BrowserCacheLog.CacheOperationFailed(_logger, ex, "Invalidate", cacheKey);
        }
    }

    /// <inheritdoc />
    public async Task Clear(CancellationToken ct)
    {
        try
        {
            BrowserCacheLog.CacheClearAttempt(_logger);

            await _jsRuntime.InvokeVoidAsync(
                "fdwCache.clear",
                ct).ConfigureAwait(false);

            BrowserCacheLog.CacheClearCompleted(_logger);
        }
        catch (JSException ex)
        {
            BrowserCacheLog.CacheOperationFailed(_logger, ex, "Clear", "all");
        }
    }

    /// <summary>
    /// Internal DTO for serializing cache entries to/from IndexedDB.
    /// </summary>
    private sealed class CacheEntry
    {
        /// <summary>
        /// Gets or sets the cache key.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serialized data JSON.
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ETag, if any.
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the entry was cached.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
    }
}
