using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The groups of data sets and containers the caller can see, and the lines pipelines draw between them.</summary>
public sealed class LineageGroupsResponse
{
    /// <summary>Gets or sets the groups: categories with the categories above them, the data sets under none, and the containers by data store path.</summary>
    public IReadOnlyList<LineageGroupDto> Groups { get; set; } = [];

    /// <summary>Gets or sets the lines between two different groups. A pipeline whose ends sit in one group draws none.</summary>
    public IReadOnlyList<LineageLineDto> Lines { get; set; } = [];
}
