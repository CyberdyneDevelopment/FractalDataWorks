using FastEndpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Asks for the groups of data sets and containers, and the lines pipelines draw between them.</summary>
public sealed class LineageGroupsRequest
{
    /// <summary>Gets or sets how data sets are grouped: category or tag. Required; nothing is assumed when it is left out.</summary>
    [QueryParam]
    public string? GroupBy { get; set; }

    /// <summary>
    /// Gets or sets the name of a data-set category: only that group and the groups under it are answered. Omitted means every group.
    /// Valid only when grouping by category.
    /// </summary>
    [QueryParam]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets where the containers pipelines use sit when grouping by tag: storePath keeps them in their data store path group, storeTags puts
    /// a container in the group of each tag its data store carries. Required with groupBy=tag; not valid with groupBy=category. Nothing is assumed.
    /// </summary>
    [QueryParam]
    public string? ContainerGroups { get; set; }

    /// <summary>
    /// Gets or sets the name of a tag: only the data sets that carry it are counted, grouped by the other tags they carry. Omitted means every tag.
    /// Valid only when grouping by tag.
    /// </summary>
    [QueryParam]
    public string? Tag { get; set; }

    /// <summary>Gets or sets the workspace to scope the answer to; omitted means everything the caller can see.</summary>
    [QueryParam]
    public string? Workspace { get; set; }
}
