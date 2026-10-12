using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a map would hold, counted without reading it: the answer to <c>GET /map/summary</c>.</summary>
/// <remarks>
/// A count that cannot be had cheaply is <see langword="null"/> and named in <see cref="NotCounted"/>: never 0, never a
/// guess. A client that wants an exact count of a kind that is not counted reads the map for it.
/// </remarks>
public sealed class MapSummaryResponse
{
    /// <summary>Gets or sets what the counts cover, as a map response says what it covers.</summary>
    public MapScopeDto Scope { get; set; } = new();

    /// <summary>Gets or sets the count of each kind of node the map can hold, keyed by kind name; nothing for a kind not counted.</summary>
    public IDictionary<string, int?> Nodes { get; set; } = new Dictionary<string, int?>(StringComparer.Ordinal);

    /// <summary>Gets or sets the count of each relation an edge can carry, keyed by relation name; nothing for a relation not counted.</summary>
    public IDictionary<string, int?> Edges { get; set; } = new Dictionary<string, int?>(StringComparer.Ordinal);

    /// <summary>Gets or sets the counts that are not given, as <c>nodes.{kind}</c> or <c>edges.{relation}</c>.</summary>
    public List<string> NotCounted { get; set; } = [];
}
