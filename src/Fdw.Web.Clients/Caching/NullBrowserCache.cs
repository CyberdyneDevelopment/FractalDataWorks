namespace Fdw.Web.Clients.Abstractions.Caching;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// No-op implementation of <see cref="IBrowserCache"/> for Server-side rendering mode.
/// All operations complete immediately without storing or retrieving data.
/// </summary>
public sealed class NullBrowserCache : IBrowserCache
{
    /// <inheritdoc />
    public Task<CachedResponse<T>?> Get<T>(string cacheKey, CancellationToken ct)
    {
        return Task.FromResult<CachedResponse<T>?>(null);
    }

    /// <inheritdoc />
    public Task Set<T>(string cacheKey, T data, string? etag, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Invalidate(string cacheKey, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Clear(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
