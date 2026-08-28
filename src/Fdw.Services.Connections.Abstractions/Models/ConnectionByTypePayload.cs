namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// A connection of a particular type, as the by-type listing returns it.
/// </summary>
public sealed class ConnectionByTypePayload
{
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection type.</summary>
    public string ConnectionType { get; set; } = string.Empty;
}
