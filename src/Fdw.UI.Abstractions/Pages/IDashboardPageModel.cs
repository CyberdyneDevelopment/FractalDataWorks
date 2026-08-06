using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a dashboard page with status widgets and quick actions.
/// </summary>
/// <remarks>
/// Dashboards provide an overview of system state including:
/// - Status indicators for services
/// - Quick stats and metrics
/// - Recent activity
/// - Quick action shortcuts
/// </remarks>
public interface IDashboardPageModel
{
    /// <summary>
    /// Gets the unique identifier for this dashboard.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the dashboard title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the dashboard description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the status widgets displayed on the dashboard.
    /// </summary>
    IReadOnlyList<IStatusWidget> StatusWidgets { get; }

    /// <summary>
    /// Gets the metric widgets displayed on the dashboard.
    /// </summary>
    IReadOnlyList<IMetricWidget> MetricWidgets { get; }

    /// <summary>
    /// Gets the quick action buttons.
    /// </summary>
    IReadOnlyList<IPageAction> QuickActions { get; }

    /// <summary>
    /// Gets the recent activity items.
    /// </summary>
    IReadOnlyList<IActivityItem> RecentActivity { get; }

    /// <summary>
    /// Gets the timestamp when the dashboard was last refreshed.
    /// </summary>
    DateTime LastRefreshed { get; }

    /// <summary>
    /// Gets the auto-refresh interval in seconds (0 = disabled).
    /// </summary>
    int AutoRefreshSeconds { get; }
}