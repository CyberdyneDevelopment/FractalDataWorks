namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Detaches a resource from a dataverse.</summary>
public class DetachDataverseResourceRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the attachment's own identity — the node id the map returned, not the resource's own id.</summary>
    public System.Guid ResourceAttachmentId { get; set; }
}
