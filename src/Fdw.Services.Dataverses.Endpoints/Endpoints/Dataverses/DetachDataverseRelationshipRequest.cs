namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Removes a declared relationship from a dataverse.</summary>
public class DetachDataverseRelationshipRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the relationship's own identity — the edge id the map returned.</summary>
    public System.Guid RelationshipId { get; set; }
}
