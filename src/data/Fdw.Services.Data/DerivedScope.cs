using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>
/// The invalidation tags of the reads one build made, each with the cache's clock as its first read began.
/// </summary>
/// <remarks>
/// One scope belongs to one build and travels with that build's async flow, including the tasks it
/// starts, so a read made by another request while the build runs is neither recorded here nor missing
/// from another build's scope. Concurrent, because a build may read in parallel. A tag keeps the clock of
/// its first read, the earliest: the build is not kept when the tag was invalidated after any of its reads
/// under that tag began (FDW-4782).
/// </remarks>
internal sealed class DerivedScope
{
    private readonly ConcurrentDictionary<string, long> _readFrom = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records tags a read of this build is cached under, and the cache's clock as the read begins.</summary>
    internal void Note(IReadOnlyList<string> tags, long readFrom)
    {
        for (var i = 0; i < tags.Count; i++)
            _ = _readFrom.TryAdd(tags[i], readFrom);
    }

    /// <summary>Every tag recorded so far, with the clock its first read took.</summary>
    internal IReadOnlyDictionary<string, long> ReadFrom()
        => new Dictionary<string, long>(_readFrom, StringComparer.OrdinalIgnoreCase);
}
