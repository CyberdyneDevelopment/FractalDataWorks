namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request naming an organization whose features are read (FDW-1580).
/// </summary>
public class OrganizationFeaturesRequest
{
    /// <summary>Gets or sets the organization.</summary>
    public System.Guid OrganizationId { get; set; }
}
