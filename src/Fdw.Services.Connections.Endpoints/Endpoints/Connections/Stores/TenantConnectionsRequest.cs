namespace Fdw.Services.Connections.Endpoints;

/// <summary>A request about the connections granted to one tenant (FDW-1581).</summary>
public class TenantConnectionsRequest
{
    /// <summary>Gets or sets the tenant, from the route.</summary>
    public System.Guid TenantId { get; set; }
}
