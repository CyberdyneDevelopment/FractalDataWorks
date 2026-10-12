using FastEndpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Asks how one node reaches another.</summary>
public sealed class LineagePathRequest
{
    /// <summary>Gets or sets the node data starts from (DataSet:id or DataStore:id). Required.</summary>
    [QueryParam]
    public string? From { get; set; }

    /// <summary>Gets or sets the node data should reach (DataSet:id or DataStore:id). Required.</summary>
    [QueryParam]
    public string? To { get; set; }

    /// <summary>Gets or sets the most steps to search, 1 to 8. Omitted means 8.</summary>
    [QueryParam]
    public int? MaxDepth { get; set; }

    /// <summary>Gets or sets the workspace to scope the search to; omitted means everything the caller can see.</summary>
    [QueryParam]
    public string? Workspace { get; set; }
}
