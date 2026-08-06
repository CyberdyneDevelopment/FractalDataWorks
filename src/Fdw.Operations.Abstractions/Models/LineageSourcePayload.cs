namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Upstream source in lineage.
/// </summary>
public sealed class LineageSourcePayload
{
    /// <summary>Gets or sets the source name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the source type.</summary>
    public string SourceType { get; set; } = string.Empty;
    /// <summary>Gets or sets the associated connection name.</summary>
    public string? ConnectionName { get; set; }
    /// <summary>Gets or sets the associated data store name.</summary>
    public string? DataStoreName { get; set; }
    /// <summary>Gets or sets the physical location.</summary>
    public string? PhysicalLocation { get; set; }
    /// <summary>Gets or sets the source priority.</summary>
    public int Priority { get; set; }
}
