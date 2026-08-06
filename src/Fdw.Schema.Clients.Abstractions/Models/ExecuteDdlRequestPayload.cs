namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Request to execute a DDL script.
/// </summary>
public sealed class ExecuteDdlRequestPayload
{
    /// <summary>Gets or sets the connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the DDL script to execute.</summary>
    public string Ddl { get; set; } = string.Empty;
}
