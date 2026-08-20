namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// A connection of a particular type, as the by-type listing returns it.
/// </summary>
// Why this is its own type rather than ConnectionPayload: the by-type listing returns a name and a
// type and nothing else. Deserialising it into ConnectionPayload would succeed and leave six
// properties at their defaults, which reads as a connection that has never been tested or
// discovered rather than one whose details were never asked for.
public sealed class ConnectionByTypePayload
{
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection type.</summary>
    public string ConnectionType { get; set; } = string.Empty;
}
