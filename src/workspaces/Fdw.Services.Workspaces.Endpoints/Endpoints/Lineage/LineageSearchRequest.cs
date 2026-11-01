using FastEndpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Asks for the nodes whose names contain some text.</summary>
public sealed class LineageSearchRequest
{
    /// <summary>Gets or sets the text to find in names, ignoring case. Required.</summary>
    [QueryParam]
    public string? Q { get; set; }

    /// <summary>Gets or sets the kinds to search, comma separated: DataSet, DataStore, Pipeline. Omitted means all three.</summary>
    [QueryParam]
    public string? Kinds { get; set; }

    /// <summary>Gets or sets how many matches to return, 1 to 100. Omitted means 20.</summary>
    [QueryParam]
    public int? Take { get; set; }

    /// <summary>Gets or sets the workspace to scope the search to; omitted means everything the caller can see.</summary>
    [QueryParam]
    public string? Workspace { get; set; }

    /// <summary>Gets or sets how many matches precede the page. Omitted means 0.</summary>
    [QueryParam]
    public int? Skip { get; set; }

    /// <summary>Gets or sets how a name must match: <c>contains</c> (the default) or <c>startsWith</c>.</summary>
    [QueryParam]
    public string? Match { get; set; }

    /// <summary>Gets or sets whether the answer is the paged envelope, with the path that leads to each hit, where the default is the bare list of hits.</summary>
    [QueryParam]
    public bool? Paged { get; set; }
}
