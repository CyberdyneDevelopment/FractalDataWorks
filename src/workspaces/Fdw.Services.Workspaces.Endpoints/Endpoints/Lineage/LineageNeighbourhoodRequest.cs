using FastEndpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Asks for the neighbourhood of one node.</summary>
public sealed class LineageNeighbourhoodRequest
{
    /// <summary>Gets or sets the focus node id (DataSet:id, DataStore:id or Pipeline:id). Required.</summary>
    [QueryParam]
    public string? Node { get; set; }

    /// <summary>Gets or sets the direction: up, down or both. Required.</summary>
    [QueryParam]
    public string? Direction { get; set; }

    /// <summary>Gets or sets how many steps to walk, 1 to 5. Required.</summary>
    [QueryParam]
    public int? Depth { get; set; }

    /// <summary>Gets or sets the kinds to include, comma separated: DataSet, DataStore, Pipeline, Connection. Omitted means DataSet,Pipeline.</summary>
    [QueryParam]
    public string? Kinds { get; set; }

    /// <summary>Gets or sets the workspace to scope the answer to; omitted means everything the caller can see.</summary>
    [QueryParam]
    public string? Workspace { get; set; }
}
