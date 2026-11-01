using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Response for <c>GET /datasets/{DataSetName}/workspaces</c>.</summary>
public sealed class DataSetWorkspacesResponse
{
    /// <summary>Gets or sets the data set asked about.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the workspaces holding it that the caller may see, ordered by name.</summary>
    public IReadOnlyList<DataSetWorkspaceDto> Workspaces { get; set; } = [];
}
