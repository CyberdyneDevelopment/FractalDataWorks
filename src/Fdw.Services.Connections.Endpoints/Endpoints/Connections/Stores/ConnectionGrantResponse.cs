namespace Fdw.Services.Connections.Endpoints;

/// <summary>A connection granted to a tenant (FDW-1581).</summary>
public class ConnectionGrantResponse
{
    /// <summary>Gets or sets the grant's id, which an assignment names.</summary>
    public System.Guid Id { get; set; }

    /// <summary>Gets or sets the tenant it was granted to.</summary>
    public System.Guid TenantId { get; set; }

    /// <summary>Gets or sets the granted connection.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets what it was granted for.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets or sets the folder the grant is narrowed to; null when it grants the connection whole.</summary>
    public string? FolderPath { get; set; }

    /// <summary>Gets or sets whether the tenant may register its own connections for the purpose.</summary>
    public bool MayRegisterOwn { get; set; }
}
