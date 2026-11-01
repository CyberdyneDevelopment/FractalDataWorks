using Fdw.Services.Data.Endpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Request for <c>POST /{WorkspaceName}/{DataSetName}</c> — a query of one data set, reached
/// only through a workspace the caller may read.
/// </summary>
/// <remarks>
/// The body is the data query every route shares (<see cref="DataQueryRequest"/>); the workspace is
/// the one thing this layer adds, and it narrows which data sets the name can reach.
/// </remarks>
public sealed class QueryWorkspaceDataSetRequest : DataQueryRequest
{
    /// <summary>Gets or sets the workspace name (route).</summary>
    public string WorkspaceName { get; set; } = string.Empty;
}
