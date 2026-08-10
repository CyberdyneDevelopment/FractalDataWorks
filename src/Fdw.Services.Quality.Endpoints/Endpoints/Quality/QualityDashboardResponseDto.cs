namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// Response DTO for the quality dashboard endpoint.
/// Matches the client-side <c>QualityDashboardPayload</c> shape.
/// </summary>
public sealed class QualityDashboardResponseDto
{
    /// <summary>Gets or sets the total number of quality rules.</summary>
    public int TotalRules { get; set; }

    /// <summary>Gets or sets the number of passing quality rules.</summary>
    public int PassingRules { get; set; }

    /// <summary>Gets or sets the number of failing quality rules.</summary>
    public int FailingRules { get; set; }
}
