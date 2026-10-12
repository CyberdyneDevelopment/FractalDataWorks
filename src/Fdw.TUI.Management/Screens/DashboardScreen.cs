using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Fdw.TUI.Management.Navigation;
using Fdw.TUI.Management.Services;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Components.Pages;
using Fdw.UI.Rendering.Spectre;
using Fdw.UI.Rendering.Spectre.PageRenderers;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.TUI.Management.Screens;

/// <summary>
/// Dashboard screen showing system health and metrics.
/// </summary>
public sealed class DashboardScreen : ScreenBase
{
    private readonly IConnectionManager _connectionManager;
    private readonly SpectreRenderContext _renderContext;
    private readonly DashboardPageRenderer _dashboardRenderer;

    /// <inheritdoc />
    public override string Title => "System Dashboard";

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardScreen"/> class.
    /// </summary>
    public DashboardScreen(
        IAnsiConsole console,
        IMenuTheme theme,
        IScreenFactory screenFactory,
        IConnectionManager connectionManager,
        SpectreRenderContext renderContext)
        : base(console, theme, screenFactory)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _renderContext = renderContext ?? throw new ArgumentNullException(nameof(renderContext));
        _dashboardRenderer = new DashboardPageRenderer();
    }

    /// <inheritdoc />
    public override Task<NavigationResult> Show()
    {
        // Build the dashboard model with mock data (would come from API in real implementation)
        var dashboard = BuildDashboardModel();

        // Render using the page renderer
        var result = DashboardPageRenderer.Render(dashboard, _renderContext);

        if (result.ShouldExit)
        {
            return Task.FromResult(NavigationResult.Pop());
        }

        if (result.ShouldRefresh)
        {
            // Re-render the same screen
            return Task.FromResult(NavigationResult.Stay());
        }

        // Handle selected action
        if (result.Action != null)
        {
            return Task.FromResult(HandleAction(result.Action.Id));
        }

        return Task.FromResult(NavigationResult.Stay());
    }

    private DashboardPageModel BuildDashboardModel()
    {
        var status = _connectionManager.GetStatus();

        var dashboard = new DashboardPageModel
        {
            Id = "system-dashboard",
            Title = "System Dashboard",
            Description = $"Connected to: {status.InstanceName ?? "Unknown"}",
            LastRefreshed = DateTime.Now,
            AutoRefreshSeconds = 30
        };

        AddStatusWidgets(dashboard);
        AddMetricWidgets(dashboard);
        AddRecentActivities(dashboard);
        AddQuickActions(dashboard);

        return dashboard;
    }

    private static void AddStatusWidgets(DashboardPageModel dashboard)
    {
        // Mock data - would come from API in real implementation
        dashboard.AddStatusWidget(new StatusWidget
        {
            Id = "api-service",
            Label = "API Service",
            Status = ServiceStatuses.Healthy,
            StatusMessage = "All endpoints responding"
        });

        dashboard.AddStatusWidget(new StatusWidget
        {
            Id = "database",
            Label = "Database",
            Status = ServiceStatuses.Healthy,
            StatusMessage = "Connected"
        });

        dashboard.AddStatusWidget(new StatusWidget
        {
            Id = "scheduler",
            Label = "Scheduler",
            Status = ServiceStatuses.Healthy,
            StatusMessage = "12 jobs active"
        });

        dashboard.AddStatusWidget(new StatusWidget
        {
            Id = "messaging",
            Label = "Message Queue",
            Status = ServiceStatuses.Degraded,
            StatusMessage = "High latency detected"
        });
    }

    private static void AddMetricWidgets(DashboardPageModel dashboard)
    {
        // Mock data - would come from API in real implementation
        dashboard.AddMetricWidget(new MetricWidget
        {
            Id = "active-pipelines",
            Label = "Active Pipelines",
            Value = 8,
            Icon = "⚙",
            Trend = TrendDirections.Up,
            TrendPercentage = 14.5m
        });

        dashboard.AddMetricWidget(new MetricWidget
        {
            Id = "records-processed",
            Label = "Records Processed",
            Value = 1247832,
            FormatString = "N0",
            Icon = "📊",
            Trend = TrendDirections.Up,
            TrendPercentage = 8.2m
        });

        dashboard.AddMetricWidget(new MetricWidget
        {
            Id = "error-rate",
            Label = "Error Rate",
            Value = 0.23m,
            FormatString = "P2",
            Icon = "⚠",
            Trend = TrendDirections.Down,
            TrendPercentage = -15.0m
        });

        dashboard.AddMetricWidget(new MetricWidget
        {
            Id = "avg-latency",
            Label = "Avg Latency",
            Value = 142,
            Unit = "ms",
            Icon = "⏱",
            Trend = TrendDirections.Stable
        });
    }

    private static void AddRecentActivities(DashboardPageModel dashboard)
    {
        // Mock data - would come from API in real implementation
        dashboard.AddActivity(new ActivityItem
        {
            ActivityType = "Success",
            Message = "Pipeline 'DailyImport' completed successfully",
            Timestamp = DateTime.Now.AddMinutes(-5),
            Severity = ActivitySeverities.Success,
            User = "scheduler"
        });

        dashboard.AddActivity(new ActivityItem
        {
            ActivityType = "Info",
            Message = "New DataSet 'CustomerAnalytics' created",
            Timestamp = DateTime.Now.AddMinutes(-15),
            Severity = ActivitySeverities.Info,
            User = "admin@company.com"
        });

        dashboard.AddActivity(new ActivityItem
        {
            ActivityType = "Warning",
            Message = "Connection timeout to external API",
            Timestamp = DateTime.Now.AddMinutes(-22),
            Severity = ActivitySeverities.Warning,
            User = "system"
        });

        dashboard.AddActivity(new ActivityItem
        {
            ActivityType = "Info",
            Message = "Workflow 'ApprovalProcess' started",
            Timestamp = DateTime.Now.AddMinutes(-30),
            Severity = ActivitySeverities.Info,
            User = "workflow-engine"
        });

        dashboard.AddActivity(new ActivityItem
        {
            ActivityType = "Info",
            Message = "Configuration updated: Email notifications",
            Timestamp = DateTime.Now.AddHours(-1),
            Severity = ActivitySeverities.Info,
            User = "admin@company.com"
        });
    }

    private static void AddQuickActions(DashboardPageModel dashboard)
    {
        dashboard.AddQuickAction(new PageAction
        {
            Id = "view-logs",
            Label = "View Logs",
            Icon = "📋",
            Shortcut = 'l'
        });

        dashboard.AddQuickAction(new PageAction
        {
            Id = "view-pipelines",
            Label = "Pipeline Status",
            Icon = "⚙",
            Shortcut = 'p'
        });
    }

    private NavigationResult HandleAction(string actionId)
    {
        switch (actionId)
        {
            case "view-logs":
                Console.MarkupLine($"[{Theme.Colors.Info}]Opening log viewer...[/]");
                System.Console.ReadKey(true);
                return NavigationResult.Stay();

            case "view-pipelines":
                Console.MarkupLine($"[{Theme.Colors.Info}]Opening pipeline status...[/]");
                System.Console.ReadKey(true);
                return NavigationResult.Stay();

            default:
                return NavigationResult.Stay();
        }
    }
}
