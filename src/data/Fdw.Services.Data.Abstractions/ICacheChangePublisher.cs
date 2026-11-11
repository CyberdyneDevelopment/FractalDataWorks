using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Tells the other processes that read the same configuration which of it changed, so each drops its cached reads of
/// it (FDW-4782).
/// </summary>
/// <remarks>
/// A configuration gateway calls this after a write has committed, with the invalidation tags the write changed. It
/// never blocks the write and never fails it: the tags are queued and written out in the background, and a write that
/// cannot be published leaves the other processes' caches stale for at most their cache duration, which is logged.
/// </remarks>
public interface ICacheChangePublisher
{
    /// <summary>Queues the tags a committed write changed, to be published to the other processes.</summary>
    /// <param name="tags">The invalidation tags (e.g., "conn.Connection").</param>
    void Publish(IReadOnlyList<string> tags);
}
