namespace Fdw.Services.Connections.Endpoints;

/// <summary>The connections assigned to an organization (FDW-1581).</summary>
public class OrganizationConnectionsResponse
{
    /// <summary>Gets or sets the assignments.</summary>
    public System.Collections.Generic.IList<OrganizationConnectionResponse> Connections { get; set; } = [];
}
