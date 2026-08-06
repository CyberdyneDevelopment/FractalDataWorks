using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Invalidates cached DataGateway command results by tag.
/// Writers call this after persisting changes to ensure stale data is evicted.
/// </summary>
// Why: Decouples writers from providers. Writers invalidate by "{schema}.{table}" tag
// instead of resolving provider singletons and calling EvictAllFromUserCache().
public interface ICacheInvalidator
{
    /// <summary>
    /// Invalidates all cached results tagged with the specified tag.
    /// </summary>
    /// <param name="tag">The invalidation tag (e.g., "conn.Connection").</param>
    void InvalidateByTag(string tag);

    /// <summary>
    /// Invalidates all cached results tagged with any of the specified tags.
    /// </summary>
    /// <param name="tags">The invalidation tags.</param>
    void InvalidateByTags(IEnumerable<string> tags);

    /// <summary>
    /// Invalidates all cached results.
    /// </summary>
    void InvalidateAll();
}
