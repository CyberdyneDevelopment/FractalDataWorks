using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a dashboard page model.
/// </summary>
public sealed class DashboardPageModel : IDashboardPageModel
{
    private readonly List<StatusWidget> _statusWidgets = [];
    private readonly List<MetricWidget> _metricWidgets = [];
    private readonly List<PageAction> _quickActions = [];
    private readonly List<ActivityItem> _recentActivity = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IStatusWidget> StatusWidgets => _statusWidgets;

    /// <inheritdoc />
    public IReadOnlyList<IMetricWidget> MetricWidgets => _metricWidgets;

    /// <inheritdoc />
    public IReadOnlyList<IPageAction> QuickActions => _quickActions;

    /// <inheritdoc />
    public IReadOnlyList<IActivityItem> RecentActivity => _recentActivity;

    /// <inheritdoc />
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public int AutoRefreshSeconds { get; set; } = 30;

    /// <summary>
    /// Adds a status widget.
    /// </summary>
    public void AddStatusWidget(StatusWidget widget) => _statusWidgets.Add(widget);

    /// <summary>
    /// Adds a metric widget.
    /// </summary>
    public void AddMetricWidget(MetricWidget widget) => _metricWidgets.Add(widget);

    /// <summary>
    /// Adds a quick action.
    /// </summary>
    public void AddQuickAction(PageAction action) => _quickActions.Add(action);

    /// <summary>
    /// Adds an activity item.
    /// </summary>
    public void AddActivity(ActivityItem item) => _recentActivity.Add(item);

    /// <summary>
    /// Clears all activity items.
    /// </summary>
    public void ClearActivity() => _recentActivity.Clear();
}