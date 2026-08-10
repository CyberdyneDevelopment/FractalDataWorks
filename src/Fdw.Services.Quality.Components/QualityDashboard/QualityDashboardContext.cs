using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Quality.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Quality.Components.QualityDashboard;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="QualityDashboardProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class QualityDashboardContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the dashboard statistics.</summary>
    public QualityDashboardPayload? Dashboard { get; init; }

    /// <summary>Gets the list of recent quality check results.</summary>
    public IReadOnlyList<QualityRuleSummaryPayload> RecentExecutions { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

}
