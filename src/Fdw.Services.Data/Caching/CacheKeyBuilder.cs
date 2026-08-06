using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>
/// Computes cache keys and invalidation tags for DataGateway result caching.
/// Addressing (DataStore, Path, Container) now lives in <see cref="DataStoreTarget"/> rather than
/// on <see cref="IDataCommand"/>, so this class accepts both.
/// </summary>
public static class CacheKeyBuilder
{
    /// <summary>
    /// Gets the cache key prefix from command Metadata, or derives one from the target's path and container.
    /// </summary>
    /// <param name="command">The data command (for metadata override check).</param>
    /// <param name="target">The DataStore/Path/Container address.</param>
    /// <returns>A string prefix suitable for use in a cache key.</returns>
    public static string GetKeyPrefix(IDataCommand command, DataStoreTarget target)
    {
        if (command.Metadata.TryGetValue(CachePolicy.CacheKeyPrefixKey, out var val) && val is string prefix)
            return prefix;

        // Why: Default tag format matches invalidation convention: "{schema}.{table}"
        return string.IsNullOrEmpty(target.Path)
            ? target.Container
            : string.Concat(target.Path, ".", target.Container);
    }

    /// <summary>
    /// Gets the invalidation tags from command Metadata, or derives a default from the target's path and container.
    /// </summary>
    /// <param name="command">The data command (for metadata override check).</param>
    /// <param name="target">The DataStore/Path/Container address.</param>
    /// <returns>A read-only list of cache invalidation tags.</returns>
    public static IReadOnlyList<string> GetInvalidationTags(IDataCommand command, DataStoreTarget target)
    {
        if (command.Metadata.TryGetValue(CachePolicy.CacheInvalidationTagsKey, out var val) && val is string[] tags)
            return tags;

        // Why: Default tag is "{schema}.{table}" — matches the convention writers use to invalidate.
        return new[] { GetKeyPrefix(command, target) };
    }

    /// <summary>
    /// Computes a deterministic cache key from the target address and command's semantic fields.
    /// Does NOT include typeof(T) — the caller (DataGatewayService) appends that.
    /// </summary>
    // Why: Cache key must be deterministic from the query shape, not from object identity.
    // CommandId and CreatedAt are unique per instance and must be excluded.
    public static string ComputeCacheKey(IDataCommand command, DataStoreTarget target)
    {
        var sb = new StringBuilder(128);
        sb.Append(GetKeyPrefix(command, target));
        sb.Append(':');
        sb.Append(target.DataStore);

        if (command is IQueryCommand query)
        {
            if (query.Filter?.Root != null)
            {
                sb.Append(":f=");
                sb.Append(query.Filter.Root.GetHashCode().ToString("x8", CultureInfo.InvariantCulture));
            }

            if (query.Ordering != null)
            {
                sb.Append(":o=");
                sb.Append(query.Ordering.GetHashCode().ToString("x8", CultureInfo.InvariantCulture));
            }

            if (query.Paging != null)
            {
                sb.Append(":p=");
                sb.Append(query.Paging.Skip);
                sb.Append(',');
                sb.Append(query.Paging.Take);
            }
        }

        return sb.ToString();
    }
}
