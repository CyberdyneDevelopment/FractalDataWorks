namespace Fdw.Services.Quality.Clients.Models;

/// <summary>
/// Quality dashboard data.
/// </summary>
// Why: pure data-transfer POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class QualityDashboardPayload
{
    /// <summary>Gets or sets the total number of rules.</summary>
    public int TotalRules { get; set; }

    /// <summary>Gets or sets the number of passing rules.</summary>
    public int PassingRules { get; set; }

    /// <summary>Gets or sets the number of failing rules.</summary>
    public int FailingRules { get; set; }
}
