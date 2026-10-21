namespace Fdw.Services.Connections.Endpoints;

/// <summary>A request to assign an organization one of its tenant's grants for a purpose (FDW-1581).</summary>
public class OrganizationConnectionRequest : OrganizationConnectionKeyRequest
{
    /// <summary>Gets or sets the grant to use, which must be one the organization's tenant holds.</summary>
    public System.Guid GrantId { get; set; }

    /// <summary>Gets or sets the folder inside the grant's the organization is narrowed to; leave out to use the grant as it is.</summary>
    public string? BaseFolder { get; set; }
}
