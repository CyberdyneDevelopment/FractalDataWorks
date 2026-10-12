namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Request for <c>GET /openapi/data/{WorkspaceName}</c> — the API reference of one workspace.
/// </summary>
public sealed class WorkspaceReferenceRequest
{
    /// <summary>Gets or sets the workspace name (route).</summary>
    public string WorkspaceName { get; set; } = string.Empty;
}
