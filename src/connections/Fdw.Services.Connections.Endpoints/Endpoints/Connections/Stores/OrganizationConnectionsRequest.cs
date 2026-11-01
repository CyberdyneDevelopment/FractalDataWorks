namespace Fdw.Services.Connections.Endpoints;

/// <summary>A request about the connections assigned to one organization (FDW-1581).</summary>
public class OrganizationConnectionsRequest
{
    /// <summary>Gets or sets the organization, from the route.</summary>
    public System.Guid OrganizationId { get; set; }
}
