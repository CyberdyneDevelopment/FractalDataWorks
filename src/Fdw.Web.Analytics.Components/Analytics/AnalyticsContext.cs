#pragma warning disable CS1591
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.Analytics;

// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class AnalyticsContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public AnalyticsResponse? Data { get; init; }
    public AnalyticsRequest CurrentRequest { get; init; } = new();

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadAnalytics { get; init; } = () => Task.CompletedTask;
    public Func<DateTimeOffset, DateTimeOffset, Task> OnUpdatePeriod { get; init; } = (_, _) => Task.CompletedTask;
}
