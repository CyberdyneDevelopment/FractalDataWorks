using Fdw.Data.Abstractions;

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

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the path's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;
}
