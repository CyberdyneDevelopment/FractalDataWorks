namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Represents a connection that supports schema discovery.
/// </summary>
public sealed class SchemaCapableConnectionPayload
{
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the connection type.</summary>
    public string ConnectionType { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the connection is available for discovery.</summary>
    public bool IsAvailable { get; set; }
}
