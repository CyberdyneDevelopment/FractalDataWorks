namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Response for a connection configuration.
/// </summary>
public sealed class ConnectionResponse
{
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the connection type.</summary>
    public string ConnectionType { get; set; } = string.Empty;
}
