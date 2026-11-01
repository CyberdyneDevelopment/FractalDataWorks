namespace Fdw.Services.Connections.Endpoints;

/// <summary>The connection an organization uses for a purpose (FDW-1581).</summary>
public class OrganizationConnectionResponse
{
    /// <summary>Gets or sets the organization.</summary>
    public System.Guid OrganizationId { get; set; }

    /// <summary>Gets or sets what the connection is used for.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets or sets the grant the assignment uses.</summary>
    public System.Guid GrantId { get; set; }

    /// <summary>Gets or sets the granted connection; null when the grant no longer exists.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets the folder of the grant.</summary>
    public string? FolderPath { get; set; }

    /// <summary>Gets or sets the folder inside the grant's the organization is narrowed to.</summary>
    public string? BaseFolder { get; set; }
}
