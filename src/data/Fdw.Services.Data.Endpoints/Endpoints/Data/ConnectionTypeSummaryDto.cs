using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Summary DTO for a connection type.
/// </summary>
public class ConnectionTypeSummaryDto : ResourceSummary
{
    /// <summary>Gets or sets the display name for the connection type.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the connection type.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the category of the connection type.</summary>
    public string Category { get; set; } = string.Empty;
}
