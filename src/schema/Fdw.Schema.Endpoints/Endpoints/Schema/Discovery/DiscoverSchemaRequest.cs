namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Request for schema discovery.
/// </summary>
public class DiscoverSchemaRequest
{
    /// <summary>
    /// Gets or sets the connection name (from route).
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;
}
