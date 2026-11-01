namespace Fdw.Schema.Endpoints.Ddl;

/// <summary>
/// Request to generate DDL for a connection's schema.
/// </summary>
public class GenerateDdlRequest
{
    /// <summary>Gets or sets the connection name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets optional schema filter.</summary>
    public string? SchemaFilter { get; set; }
}
