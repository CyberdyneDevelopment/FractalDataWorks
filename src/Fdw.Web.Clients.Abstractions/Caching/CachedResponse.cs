namespace Fdw.Web.Clients.Abstractions.Caching;

using System;

/// <summary>
/// Represents a cached HTTP response with metadata.
/// </summary>
/// <typeparam name="T">The type of the cached data.</typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class CachedResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CachedResponse{T}"/> class.
    /// </summary>
    /// <param name="data">The cached response data.</param>
    /// <param name="etag">The ETag associated with the cached response, if any.</param>
    /// <param name="cachedAt">The timestamp when the response was cached.</param>
    public CachedResponse(T data, string? etag, DateTimeOffset cachedAt)
    {
        Data = data;
        ETag = etag;
        CachedAt = cachedAt;
    }

    /// <summary>
    /// Gets the cached response data.
    /// </summary>
    public T Data { get; }

    /// <summary>
    /// Gets the ETag associated with the cached response, if any.
    /// </summary>
    public string? ETag { get; }

    /// <summary>
    /// Gets the timestamp when the response was cached.
    /// </summary>
    public DateTimeOffset CachedAt { get; }
}
