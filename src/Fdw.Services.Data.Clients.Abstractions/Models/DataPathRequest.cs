namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to create a data path.
/// </summary>
public sealed class DataPathRequest
{
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical path value.</summary>
    public string PhysicalPath { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
}
