using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>
/// The invalidation tags of the reads one build made.
/// </summary>
/// <remarks>
/// One scope belongs to one build and travels with that build's async flow, including the tasks it
/// starts, so a read made by another request while the build runs is neither recorded here nor missing
/// from another build's scope. Concurrent, because a build may read in parallel.
/// </remarks>
internal sealed class DerivedScope
{
    private readonly ConcurrentDictionary<string, byte> _tags = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records tags a read of this build would be cached under.</summary>
    internal void Note(IReadOnlyList<string> tags)
    {
        for (var i = 0; i < tags.Count; i++)
            _ = _tags.TryAdd(tags[i], 0);
    }

    /// <summary>Every tag recorded so far.</summary>
    internal IReadOnlyList<string> Tags() => [.. _tags.Keys];
}
