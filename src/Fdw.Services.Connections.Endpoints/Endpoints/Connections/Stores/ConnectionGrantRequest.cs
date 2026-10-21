namespace Fdw.Services.Connections.Endpoints;

/// <summary>A request to grant a tenant a document connection for a purpose (FDW-1581).</summary>
public class ConnectionGrantRequest : TenantConnectionsRequest
{
    /// <summary>Gets or sets the document connection to grant.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets what it is granted for (for example Documents).</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets or sets the folder the grant is narrowed to; leave out to grant the connection whole.</summary>
    public string? Folder { get; set; }

    /// <summary>Gets or sets whether the tenant may register its own connections for the purpose.</summary>
    public bool MayRegisterOwn { get; set; }
}
