using FastEndpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Request for <c>GET /map/nodes/{NodeId}/children</c>: the node to explode, and which of its children.</summary>
/// <remarks>
/// With none of the optional parameters the answer is every child a node has, as it always was. Children are taken in name
/// order, so a page is a page of something in a fixed order whatever was asked.
/// </remarks>
public sealed class MapChildrenRequest
{
    /// <summary>Gets or sets the id of the node to explode, as the map issued it (route).</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Gets or sets how many children precede the page. Omitted means 0.</summary>
    [QueryParam]
    public int? Skip { get; set; }

    /// <summary>Gets or sets how many children the page holds at most. Omitted means every remaining child.</summary>
    [QueryParam]
    public int? Take { get; set; }

    /// <summary>Gets or sets the text a child's name must contain, ignoring case. Omitted means every child.</summary>
    [QueryParam]
    public string? Q { get; set; }

    /// <summary>
    /// Gets or sets the kinds of child to send, comma separated: <c>Container</c> (the default) and, for a data set,
    /// <c>DataSetField</c>. Sending a data set's fields needs the permission to read data sets, which the map's own gate does not imply.
    /// </summary>
    [QueryParam]
    public string? Kinds { get; set; }
}
