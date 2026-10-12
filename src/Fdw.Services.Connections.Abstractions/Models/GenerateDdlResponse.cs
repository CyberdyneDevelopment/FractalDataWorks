namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Response DTO containing generated DDL.
/// </summary>
public sealed class GenerateDdlResponse
{
    /// <summary>Gets or sets the connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the generated DDL script.</summary>
    public string Ddl { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of statements.</summary>
    public int StatementCount { get; set; }
}
