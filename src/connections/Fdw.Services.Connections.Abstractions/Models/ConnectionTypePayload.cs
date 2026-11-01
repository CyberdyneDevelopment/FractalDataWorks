namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Connection type information.
/// </summary>
public sealed class ConnectionTypePayload
{
    /// <summary>Gets or sets the type identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the type name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the icon URL.</summary>
    public string? IconUrl { get; set; }
}
