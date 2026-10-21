namespace Fdw.Services.Connections.Endpoints;

/// <summary>A request about the connection an organization uses for one purpose (FDW-1581).</summary>
public class OrganizationConnectionKeyRequest : OrganizationConnectionsRequest
{
    /// <summary>Gets or sets the purpose, from the route.</summary>
    public string Purpose { get; set; } = string.Empty;
}
