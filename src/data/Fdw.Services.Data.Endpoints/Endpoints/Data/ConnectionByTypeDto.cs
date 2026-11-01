namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing a connection instance of a specific type.
/// </summary>
public class ConnectionByTypeDto
{
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection type.</summary>
    public string ConnectionType { get; set; } = string.Empty;
}
