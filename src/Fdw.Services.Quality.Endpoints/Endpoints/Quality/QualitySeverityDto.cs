namespace Fdw.Services.Quality.Endpoints;

/// <summary>One severity a caller may name.</summary>
public class QualitySeverityDto
{
    /// <summary>Gets or sets the exact value to send as <c>severity</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a breach at this severity stops processing.</summary>
    public bool BlocksProcessing { get; set; }
}
