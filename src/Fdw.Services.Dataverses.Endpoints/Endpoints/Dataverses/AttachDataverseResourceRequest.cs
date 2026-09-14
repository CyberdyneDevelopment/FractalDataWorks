namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Attaches a resource to a dataverse.</summary>
public class AttachDataverseResourceRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of resource attached — DataSet, DataStore, Pipeline, Calculation, SavedView.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the attached resource's logical identity.</summary>
    public System.Guid ResourceId { get; set; }

    /// <summary>Gets or sets how the dataverse relates to it: Owns, Uses or Produces.</summary>
    public string Relationship { get; set; } = string.Empty;
}
