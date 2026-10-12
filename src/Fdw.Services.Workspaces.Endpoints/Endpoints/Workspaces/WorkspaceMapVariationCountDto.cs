namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>How many variations of one kind a data set node on the map has: a token on the node, not a node of its own.</summary>
public class WorkspaceMapVariationCountDto
{
    /// <summary>Gets or sets the kind of variation: the implementation name, such as Scoped or DataView.</summary>
    /// <remarks>An open string: a client renders a kind it does not recognise as itself.</remarks>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets how many variations of that kind the data set has.</summary>
    public int Count { get; set; }
}
