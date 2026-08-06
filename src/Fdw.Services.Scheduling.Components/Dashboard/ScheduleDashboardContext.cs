using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Providers;

namespace Fdw.Services.Scheduling.Components.Dashboard;

/// <summary>
/// Immutable context for the schedule dashboard widget.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class ScheduleDashboardContext : ProviderContextBase
{
    /// <summary>Gets the total number of schedules.</summary>
    public int TotalSchedules { get; init; }

    /// <summary>Gets the number of active (enabled) schedules.</summary>
    public int ActiveSchedules { get; init; }



}
