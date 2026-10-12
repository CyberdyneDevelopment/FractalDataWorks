using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What one map request has produced so far: the ids already sent, what the edges are drawn from, and how it ended.</summary>
/// <remarks>
/// Owned by the one stream that writes it. Nodes are enumerated to the end before edges are, and a response is
/// serialized by one writer, so nothing here is shared between threads.
/// </remarks>
internal sealed class MapState
{
    private readonly HashSet<string> _nodes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _edges = new(StringComparer.Ordinal);

    /// <summary>Gets the logical ids of the data stores sent, by name.</summary>
    public Dictionary<string, Guid> DataStoresByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the logical ids of the data sets sent, by name.</summary>
    public Dictionary<string, Guid> DataSetsByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the links of every data set sent.</summary>
    public List<MapDataSetLinks> DataSets { get; } = [];

    /// <summary>Gets the links of every pipeline sent.</summary>
    public List<MapPipelineLinks> Pipelines { get; } = [];

    /// <summary>Gets how many nodes have been sent.</summary>
    public int NodeCount => _nodes.Count;

    /// <summary>Gets how many edges have been sent.</summary>
    public int EdgeCount => _edges.Count;

    /// <summary>Gets how many references named an object that was not there.</summary>
    public int Unresolved { get; private set; }

    /// <summary>Gets how the stream ended: success, or the first failure that cut it short.</summary>
    public IGenericResult Outcome { get; private set; } = GenericResult.Success();

    /// <summary>Records a node as sent.</summary>
    /// <param name="id">The node's id.</param>
    /// <returns><see langword="false"/> when a node with that id was already sent, so the repeat is dropped.</returns>
    public bool AddNode(string id) => _nodes.Add(id);

    /// <summary>Gets whether a node with the id has been sent.</summary>
    /// <param name="id">The node's id.</param>
    public bool HasNode(string id) => _nodes.Contains(id);

    /// <summary>Records an edge as sent.</summary>
    /// <param name="id">The edge's id.</param>
    /// <returns><see langword="false"/> when an edge with that id was already sent, so the repeat is dropped.</returns>
    public bool AddEdge(string id) => _edges.Add(id);

    /// <summary>Counts a reference that named an object that was not there.</summary>
    public void NoteUnresolved() => Unresolved++;

    /// <summary>Records the failure that cut the stream short. The first one stands.</summary>
    /// <param name="failure">The failed result.</param>
    public void Fail(IGenericResult failure)
    {
        if (Outcome.IsSuccess) Outcome = failure;
    }
}
