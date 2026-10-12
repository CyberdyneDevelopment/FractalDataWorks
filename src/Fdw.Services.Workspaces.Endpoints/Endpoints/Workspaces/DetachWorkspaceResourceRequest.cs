namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Detaches a resource from a workspace.</summary>
public class DetachWorkspaceResourceRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the attachment's own identity — the node id the map returned, not the resource's own id.</summary>
    public System.Guid ResourceAttachmentId { get; set; }
}
