namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Removes a declared relationship from a workspace.</summary>
public class DetachWorkspaceRelationshipRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the relationship's own identity — the edge id the map returned.</summary>
    public System.Guid RelationshipId { get; set; }
}
