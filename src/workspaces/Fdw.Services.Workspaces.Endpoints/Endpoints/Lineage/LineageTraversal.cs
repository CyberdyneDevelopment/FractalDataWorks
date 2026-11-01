using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The nodes one walk has placed so far, capped, remembering whether the cap stopped it.</summary>
internal sealed class LineageTraversal
{
    private readonly int _max;
    private readonly Dictionary<string, int> _depths = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="LineageTraversal"/> class.</summary>
    public LineageTraversal(int max)
    {
        _max = max;
    }

    /// <summary>Gets a value indicating whether a node was left out because the cap was reached.</summary>
    public bool Truncated { get; private set; }

    /// <summary>Gets the depth each placed node sits at.</summary>
    public Dictionary<string, int> Depths => _depths;

    /// <summary>Whether the node is already placed.</summary>
    public bool Has(string id) => _depths.ContainsKey(id);

    /// <summary>Places a node the first time it is reached; false when it was already placed or the cap stopped it.</summary>
    public bool TryPlace(string id, int depth)
    {
        if (_depths.ContainsKey(id)) return false;
        if (_depths.Count >= _max)
        {
            Truncated = true;
            return false;
        }

        _depths[id] = depth;
        return true;
    }
}
