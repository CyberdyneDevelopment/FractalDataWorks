namespace Fdw.Services.Connections.Endpoints;

/// <summary>A request about one grant of a connection to a tenant (FDW-1581).</summary>
public class ConnectionGrantKeyRequest : TenantConnectionsRequest
{
    /// <summary>Gets or sets the purpose, from the route.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection, from the route.</summary>
    public string ConnectionName { get; set; } = string.Empty;
}
