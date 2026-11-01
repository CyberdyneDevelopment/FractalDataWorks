using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One map response being produced: the nodes as they are read, then the edges drawn from what the nodes recorded.</summary>
/// <remarks>
/// The nodes are produced first and to the end, because the edges are drawn from what they recorded and only between nodes
/// that were sent. A failure part way through is recorded on <see cref="State"/> and ends the nodes: the response goes on to
/// say how it ended, since the status code is already out.
/// </remarks>
internal abstract class MapStream
{
    /// <summary>Initializes a stream.</summary>
    /// <param name="scope">What the response covers.</param>
    /// <param name="editing">What the caller may edit.</param>
    protected MapStream(MapScopeDto scope, MapEditing editing)
    {
        Scope = scope;
        Editing = editing;
    }

    /// <summary>Gets what the response covers.</summary>
    public MapScopeDto Scope { get; }

    /// <summary>Gets what the caller may edit, applied to every node and edge as it is sent.</summary>
    protected MapEditing Editing { get; }

    /// <summary>Gets what the stream has produced, and how it ended.</summary>
    public MapState State { get; } = new();

    /// <summary>Gets which part of the children was sent and whether more follow; nothing for a stream that is not a page of children. Read after <see cref="Nodes"/> has been enumerated.</summary>
    public virtual MapPageDto? Page => null;

    /// <summary>Produces the nodes, each once, as they are read.</summary>
    /// <param name="ct">The cancellation token.</param>
    public abstract IAsyncEnumerable<MapNodeDto> Nodes(CancellationToken ct = default);

    /// <summary>Produces the edges, each once. Read only after <see cref="Nodes"/> has been enumerated to the end.</summary>
    public abstract IEnumerable<MapEdgeDto> Edges();
}
