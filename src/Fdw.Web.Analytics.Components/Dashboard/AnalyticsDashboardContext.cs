using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.Dashboard;

/// <summary>
/// Immutable context for the analytics dashboard widget.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AnalyticsDashboardContext : ProviderContextBase
{
    /// <summary>Gets the analytics data for the requested date range.</summary>
    public AnalyticsResponse? AnalyticsData { get; init; }



    /// <summary>Loads analytics for the specified date range.</summary>
    public Func<DateTimeOffset, DateTimeOffset, Task> OnLoadAnalytics { get; init; } = (_, _) => Task.CompletedTask;
}
