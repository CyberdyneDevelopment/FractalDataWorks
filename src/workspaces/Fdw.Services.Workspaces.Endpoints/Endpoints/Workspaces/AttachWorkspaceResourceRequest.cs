namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Attaches a resource to a workspace.</summary>
public class AttachWorkspaceResourceRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of resource attached: a name from the resource-kind collection, which each package that owns a
    /// resource declares (data set, data store, pipeline, calculation, data view). An unknown kind is refused with the kinds
    /// this host registers; a registered kind whose label cannot be resolved yet is refused as not attachable. Today the
    /// label resolves for DataSet and DataView.
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the attached resource's logical identity.</summary>
    public System.Guid ResourceId { get; set; }

    /// <summary>Gets or sets how the workspace relates to it: Owns, Uses or Produces.</summary>
    public string Relationship { get; set; } = string.Empty;
}
