using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using FastEndpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A map, or the children of one node, written to the response as it is read.</summary>
/// <remarks>
/// The properties are written in the order declared. <see cref="Nodes"/> is written as each node is read, then
/// <see cref="Edges"/>, then the counts and <see cref="Error"/>, which are read only after both: they report how the
/// response ended. By then the status code has been sent, so a response that fails part way arrives as a 200 carrying
/// what was read before the failure, and the failure itself. A client reads <see cref="Error"/> before trusting the rest.
/// <para>
/// Every object is in <see cref="Nodes"/> once, under an id built from its kind and logical id, so a client merges
/// nodes by id whether they came from the overview or from an explode.
/// </para>
/// </remarks>
public sealed class StreamedMapResponse
{
    private readonly MapStream _stream;
    private readonly Func<ProblemDetails?> _error;

    internal StreamedMapResponse(MapStream stream, Func<ProblemDetails?> error)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>Gets what the response covers.</summary>
    [JsonPropertyOrder(0)]
    public MapScopeDto Scope => _stream.Scope;

    /// <summary>Gets the nodes, each object once, as they are read.</summary>
    [JsonPropertyOrder(1)]
    public IAsyncEnumerable<MapNodeDto> Nodes => _stream.Nodes();

    /// <summary>Gets the edges, each once, between nodes this response carries.</summary>
    [JsonPropertyOrder(2)]
    public IEnumerable<MapEdgeDto> Edges => _stream.Edges();

    /// <summary>Gets how many nodes were sent.</summary>
    [JsonPropertyOrder(3)]
    public int NodeCount => _stream.State.NodeCount;

    /// <summary>Gets how many edges were sent.</summary>
    [JsonPropertyOrder(4)]
    public int EdgeCount => _stream.State.EdgeCount;

    /// <summary>Gets how many references named an object that was not there, and so are left off the map.</summary>
    [JsonPropertyOrder(5)]
    public int Unresolved => _stream.State.Unresolved;

    /// <summary>Gets why the response ended before everything was sent, or nothing when it was all sent.</summary>
    [JsonPropertyOrder(6)]
    public ProblemDetails? Error => _error();

    /// <summary>Gets which part of the children was sent and whether more follow; left out of a response that is not a page of children.</summary>
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MapPageDto? Page => _stream.Page;
}
