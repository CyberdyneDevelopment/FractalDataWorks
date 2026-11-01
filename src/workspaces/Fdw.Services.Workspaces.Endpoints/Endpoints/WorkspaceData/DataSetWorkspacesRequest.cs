namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Request for <c>GET /datasets/{DataSetName}/workspaces</c>.</summary>
public sealed class DataSetWorkspacesRequest
{
    /// <summary>Gets or sets the data set's name (route).</summary>
    public string DataSetName { get; set; } = string.Empty;
}
