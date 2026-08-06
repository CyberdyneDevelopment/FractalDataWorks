namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Connection info for schema-capable connections.
/// </summary>
public class ConnectionInfoDto
{
    /// <summary>
    /// Gets or sets the connection name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection type (MsSql, PostgreSql, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the connection supports schema discovery.
    /// </summary>
    public bool SupportsSchemaDiscovery { get; set; }
}
