using Fdw.Services.Data.Endpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Response for <c>POST /{WorkspaceName}/{DataSetName}</c>: a data query's page, named with the workspace it was made through.</summary>
public sealed class WorkspaceDataSetQueryResponse : DataQueryResponse
{
    /// <summary>Gets or sets the workspace the query was made through.</summary>
    public string WorkspaceName { get; set; } = string.Empty;
}
