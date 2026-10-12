using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Renders dashboard pages using Spectre.Console with status widgets, metrics, and activity feed.
/// </summary>
public sealed class DashboardPageRenderer
{
    /// <summary>
    /// Renders a dashboard page and returns the selected action.
    /// </summary>
    public static DashboardPageResult Render(IDashboardPageModel dashboard, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        console.Clear();

        // Render header
        RenderHeader(dashboard, console, theme);

        // Render status widgets in a grid
        if (dashboard.StatusWidgets.Count > 0)
        {
            RenderStatusWidgets(dashboard.StatusWidgets, console, theme);
        }

        // Render metric widgets
        if (dashboard.MetricWidgets.Count > 0)
        {
            RenderMetricWidgets(dashboard.MetricWidgets, console, theme);
        }

        // Render recent activity
        if (dashboard.RecentActivity.Count > 0)
        {
            RenderActivityFeed(dashboard.RecentActivity, console, theme);
        }

        // Render quick actions
        return PromptAction(dashboard, console, theme);
    }

    private static void RenderHeader(IDashboardPageModel dashboard, IAnsiConsole console, IMenuTheme theme)
    {
        var rule = new Rule($"[{theme.Colors.Primary} bold]{dashboard.Title}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(theme.Colors.Primary)
        };
        console.Write(rule);

        if (!string.IsNullOrEmpty(dashboard.Description))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{dashboard.Description}[/]");
        }

        var refreshInfo = dashboard.AutoRefreshSeconds > 0
            ? $"Auto-refresh: {dashboard.AutoRefreshSeconds}s"
            : "Manual refresh";
        console.MarkupLine($"[{theme.Colors.Muted}]Last updated: {dashboard.LastRefreshed.ToString("g", CultureInfo.CurrentCulture)} | {refreshInfo}[/]");
        console.WriteLine();
    }

    private static void RenderStatusWidgets(IReadOnlyList<IStatusWidget> widgets, IAnsiConsole console, IMenuTheme theme)
    {
        console.MarkupLine($"[{theme.Colors.Secondary} bold]Service Status[/]");

        // Create a grid with up to 3 columns
        var grid = new Grid();
        var colCount = Math.Min(3, widgets.Count);
        for (var i = 0; i < colCount; i++)
        {
            grid.AddColumn();
        }

        var rows = (int)Math.Ceiling(widgets.Count / 3.0);
        for (var row = 0; row < rows; row++)
        {
            var rowWidgets = widgets.Skip(row * 3).Take(3).ToList();
            var panels = rowWidgets.Select(w => CreateStatusPanel(w, theme)).ToArray();

            // Pad with empty panels if needed
            while (panels.Length < colCount)
            {
                panels = panels.Append(new Panel("")).ToArray();
            }

            grid.AddRow(panels);
        }

        console.Write(grid);
        console.WriteLine();
    }

    private static Panel CreateStatusPanel(IStatusWidget widget, IMenuTheme theme)
    {
        var statusIcon = GetStatusIcon(widget.Status, theme);
        var statusColor = GetStatusColor(widget.Status, theme);

        var content = new Markup(
            $"[{statusColor}]{statusIcon}[/] [{theme.Colors.Foreground}]{widget.Label}[/]\n" +
            $"[{statusColor}]{widget.Status}[/]" +
            (string.IsNullOrEmpty(widget.StatusMessage) ? "" : $"\n[{theme.Colors.Muted}]{widget.StatusMessage}[/]"));

        return new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(statusColor),
            Padding = new Padding(1, 0)
        };
    }

    private static string GetStatusIcon(IServiceStatus status, IMenuTheme theme)
    {
        return status.Name switch
        {
            "Healthy" => theme.Icons.SuccessIcon,
            "Degraded" => theme.Icons.WarningIcon,
            "Unhealthy" => theme.Icons.ErrorIcon,
            "Offline" => "⊘",
            _ => "?"
        };
    }

    private static Color GetStatusColor(IServiceStatus status, IMenuTheme theme)
    {
        return status.Name switch
        {
            "Healthy" => theme.Colors.Success,
            "Degraded" => theme.Colors.Warning,
            "Unhealthy" => theme.Colors.Error,
            "Offline" => theme.Colors.Muted,
            _ => theme.Colors.Foreground
        };
    }

    private static void RenderMetricWidgets(IReadOnlyList<IMetricWidget> widgets, IAnsiConsole console, IMenuTheme theme)
    {
        console.MarkupLine($"[{theme.Colors.Secondary} bold]Metrics[/]");

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(theme.Colors.Muted);

        table.AddColumn(new TableColumn("Metric").LeftAligned());
        table.AddColumn(new TableColumn("Value").RightAligned());
        table.AddColumn(new TableColumn("Trend").Centered());

        foreach (var metric in widgets)
        {
            var formattedValue = metric.FormatString != null
                ? metric.Value.ToString(metric.FormatString, CultureInfo.CurrentCulture)
                : metric.Value.ToString(CultureInfo.CurrentCulture);

            var valueDisplay = !string.IsNullOrEmpty(metric.Unit)
                ? $"{formattedValue} {metric.Unit}"
                : formattedValue;

            var trendIcon = GetTrendIcon(metric.Trend, theme);
            var trendColor = GetTrendColor(metric.Trend, theme);
            var trendDisplay = metric.TrendPercentage.HasValue
                ? $"[{trendColor}]{trendIcon} {metric.TrendPercentage.Value:+0.0;-0.0}%[/]"
                : $"[{trendColor}]{trendIcon}[/]";

            var icon = metric.Icon != null ? $"{metric.Icon} " : "";

            table.AddRow(
                $"[{theme.Colors.Foreground}]{icon}{metric.Label}[/]",
                $"[{theme.Colors.Primary}]{valueDisplay}[/]",
                trendDisplay
            );
        }

        console.Write(table);
        console.WriteLine();
    }

    private static string GetTrendIcon(ITrendDirection trend, IMenuTheme theme)
    {
        return trend.Name switch
        {
            "Up" => "↑",
            "Down" => "↓",
            "Stable" => "→",
            _ => "-"
        };
    }

    private static Color GetTrendColor(ITrendDirection trend, IMenuTheme theme)
    {
        return trend.Name switch
        {
            "Up" => theme.Colors.Success,
            "Down" => theme.Colors.Error,
            "Stable" => theme.Colors.Info,
            _ => theme.Colors.Muted
        };
    }

    private static void RenderActivityFeed(IReadOnlyList<IActivityItem> activities, IAnsiConsole console, IMenuTheme theme)
    {
        console.MarkupLine($"[{theme.Colors.Secondary} bold]Recent Activity[/]");

        foreach (var activity in activities.Take(10))
        {
            var severityIcon = GetSeverityIcon(activity.Severity, theme);
            var severityColor = GetSeverityColor(activity.Severity, theme);
            var timeAgo = GetTimeAgo(activity.Timestamp);

            var userPart = !string.IsNullOrEmpty(activity.User)
                ? $" [{theme.Colors.Info}]{activity.User}[/]"
                : "";

            console.MarkupLine(
                $"[{severityColor}]{severityIcon}[/] " +
                $"[{theme.Colors.Muted}]{timeAgo}[/]{userPart} " +
                $"[{theme.Colors.Foreground}]{Markup.Escape(activity.Message)}[/]"
            );
        }

        console.WriteLine();
    }

    private static string GetSeverityIcon(IActivitySeverity severity, IMenuTheme theme)
    {
        return severity.Name switch
        {
            "Success" => theme.Icons.SuccessIcon,
            "Warning" => theme.Icons.WarningIcon,
            "Error" => theme.Icons.ErrorIcon,
            _ => theme.Icons.InfoIcon
        };
    }

    private static Color GetSeverityColor(IActivitySeverity severity, IMenuTheme theme)
    {
        return severity.Name switch
        {
            "Success" => theme.Colors.Success,
            "Warning" => theme.Colors.Warning,
            "Error" => theme.Colors.Error,
            _ => theme.Colors.Info
        };
    }

    private static string GetTimeAgo(DateTime timestamp)
    {
        var diff = DateTime.Now - timestamp;

        return diff.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{(int)diff.TotalMinutes}m ago",
            < 1440 => $"{(int)diff.TotalHours}h ago",
            < 10080 => $"{(int)diff.TotalDays}d ago",
            _ => timestamp.ToString("MMM d", CultureInfo.CurrentCulture)
        };
    }

    private static DashboardPageResult PromptAction(IDashboardPageModel dashboard, IAnsiConsole console, IMenuTheme theme)
    {
        var choices = new List<(string Id, string Label)>();

        // Quick actions
        foreach (var quickAction in dashboard.QuickActions.Where(a => a.IsEnabled))
        {
            var shortcut = quickAction.Shortcut.HasValue ? $"[{quickAction.Shortcut}] " : "";
            choices.Add((quickAction.Id, $"{shortcut}{quickAction.Label}"));
        }

        // Standard actions
        choices.Add(("refresh", "[r] Refresh"));
        choices.Add(("back", "[q] Back"));

        var prompt = new SelectionPrompt<(string Id, string Label)>()
            .Title($"[{theme.Colors.Primary}]Select action:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);

        if (string.Equals(selected.Id, "back", StringComparison.Ordinal))
        {
            return new DashboardPageResult { ShouldExit = true };
        }

        if (string.Equals(selected.Id, "refresh", StringComparison.Ordinal))
        {
            return new DashboardPageResult { ShouldExit = false, ShouldRefresh = true };
        }

        var action = dashboard.QuickActions.FirstOrDefault(a => string.Equals(a.Id, selected.Id, StringComparison.Ordinal));
        return new DashboardPageResult
        {
            ShouldExit = true,
            Action = action
        };
    }
}