namespace Fdw.Operations.Clients.Models;

using System;

/// <summary>
/// Summary information for the system dashboard.
/// </summary>
// Why: pure data-transfer POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class DashboardSummaryPayload
{
    /// <summary>Gets or sets the total number of pipelines.</summary>
    public int TotalPipelines { get; set; }
    /// <summary>Gets or sets the number of currently active pipelines.</summary>
    public int ActivePipelines { get; set; }
    /// <summary>Gets or sets the number of failed pipelines.</summary>
    public int FailedPipelines { get; set; }
    /// <summary>Gets or sets the total number of connections.</summary>
    public int TotalConnections { get; set; }
    /// <summary>Gets or sets the number of healthy connections.</summary>
    public int HealthyConnections { get; set; }
    /// <summary>Gets or sets the total number of schedules.</summary>
    public int TotalSchedules { get; set; }
    /// <summary>Gets or sets the number of active schedules.</summary>
    public int ActiveSchedules { get; set; }
    /// <summary>Gets or sets the number of records processed today.</summary>
    public int RecordsProcessedToday { get; set; }
    /// <summary>Gets or sets the timestamp of this summary.</summary>
    public DateTimeOffset AsOf { get; set; }
}
