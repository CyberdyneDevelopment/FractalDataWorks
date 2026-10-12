namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Addresses a workspace by name.</summary>
public class WorkspaceNameRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;
}
