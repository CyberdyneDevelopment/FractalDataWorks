namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Summary of a connection for display in editor dropdowns.
/// </summary>
public sealed class ConnectionSummary
{
    /// <summary>
    /// Gets or sets the connection name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection type (e.g., MsSql, PostgreSql).
    /// </summary>
    public string? Type { get; set; }
}
