namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Request for schema graph retrieval.
/// </summary>
public class GetSchemaGraphRequest
{
    /// <summary>Gets or sets the connection name (from route).</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional schema filter.</summary>
    public string? SchemaFilter { get; set; }
}
