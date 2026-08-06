using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Renders log viewer pages using Spectre.Console with level-based coloring and filtering.
/// </summary>
public sealed class LogViewerPageRenderer
{
    /// <summary>
    /// Renders a log viewer page and returns the selected action.
    /// </summary>
    public static LogViewerPageResult Render(ILogViewerPageModel page, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        console.Clear();

        // Render header with filters
        RenderHeader(page, console, theme);

        // Render filter bar
        RenderFilterBar(page, console, theme);

        // Render log entries
        RenderLogEntries(page, console, theme);

        // Render status bar
        RenderStatusBar(page, console, theme);

        // Prompt for action
        return PromptAction(page, console, theme);
    }

    private static void RenderHeader(ILogViewerPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var streamingIcon = page.IsStreaming ? "◉" : "○";
        var streamingColor = page.IsStreaming ? theme.Colors.Success : theme.Colors.Muted;

        var rule = new Rule($"[{theme.Colors.Primary} bold]{page.Title}[/] [{streamingColor}]{streamingIcon} {(page.IsStreaming ? "Live" : "Paused")}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(theme.Colors.Primary)
        };
        console.Write(rule);
        console.WriteLine();
    }

    private static void RenderFilterBar(ILogViewerPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var filters = new List<string>();

        // Level filter
        filters.Add($"Level: [{theme.Colors.Primary}]{page.MinimumLevel}+[/]");

        // Source filter
        if (!string.IsNullOrEmpty(page.SourceFilter))
        {
            filters.Add($"Source: [{theme.Colors.Info}]{page.SourceFilter}[/]");
        }

        // Search filter
        if (!string.IsNullOrEmpty(page.SearchText))
        {
            filters.Add($"Search: [{theme.Colors.Info}]\"{page.SearchText}\"[/]");
        }

        // Time range
        if (page.StartTime.HasValue || page.EndTime.HasValue)
        {
            var start = page.StartTime?.ToString("g", CultureInfo.CurrentCulture) ?? "...";
            var end = page.EndTime?.ToString("g", CultureInfo.CurrentCulture) ?? "now";
            filters.Add($"Time: [{theme.Colors.Muted}]{start} → {end}[/]");
        }

        console.MarkupLine($"[{theme.Colors.Muted}]Filters: {string.Join(" | ", filters)}[/]");
        console.WriteLine();
    }

    // MA0051: Method length acceptable - procedural log entry formatting with timestamps, levels, and exception truncation
#pragma warning disable MA0051 // Method is too long
    private static void RenderLogEntries(ILogViewerPageModel page, IAnsiConsole console, IMenuTheme theme)
#pragma warning restore MA0051
    {
        if (page.Entries.Count == 0)
        {
            console.MarkupLine($"[{theme.Colors.Muted}]No log entries match the current filters.[/]");
            console.WriteLine();
            return;
        }

        // Calculate column widths
        const int timestampWidth = 19; // "2024-01-15 10:30:45"
        const int levelWidth = 8;      // "WARNING"
        const int sourceWidth = 20;

        foreach (var entry in page.Entries)
        {
            var levelColor = GetLevelColor(entry.Level, theme);
            var levelIcon = GetLevelIcon(entry.Level, theme);
            var levelText = entry.Level.Name.ToUpperInvariant();
            if (levelText.Length > levelWidth)
            {
                levelText = new string(levelText.AsSpan(0, levelWidth));
            }

            var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var source = entry.Source ?? "";
            if (source.Length > sourceWidth)
            {
                source = string.Concat(source.AsSpan(0, sourceWidth - 3), "...");
            }

            var message = entry.Message;
            // Truncate long messages for display
            var maxMessageLen = Console.WindowWidth - timestampWidth - levelWidth - sourceWidth - 10;
            if (maxMessageLen > 20 && message.Length > maxMessageLen)
            {
                message = string.Concat(message.AsSpan(0, maxMessageLen - 3), "...");
            }

            console.MarkupLine(
                $"[{theme.Colors.Muted}]{timestamp}[/] " +
                $"[{levelColor}]{levelIcon} {levelText,-7}[/] " +
                $"[{theme.Colors.Info}]{source,-sourceWidth}[/] " +
                $"[{theme.Colors.Foreground}]{Markup.Escape(message)}[/]"
            );

            // Show exception if present (indented)
            if (!string.IsNullOrEmpty(entry.Exception))
            {
                var exceptionLines = entry.Exception.Split('\n').Take(3);
                foreach (var line in exceptionLines)
                {
                    console.MarkupLine($"[{theme.Colors.Error}]  {Markup.Escape(line.Trim())}[/]");
                }

                if (entry.Exception.Split('\n').Length > 3)
                {
                    console.MarkupLine($"[{theme.Colors.Muted}]  ... (more)[/]");
                }
            }
        }

        console.WriteLine();
    }

    private static void RenderStatusBar(ILogViewerPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var statusParts = new List<string>
        {
            $"Showing {page.Entries.Count} entries"
        };

        if (page.Pagination != null)
        {
            statusParts.Add($"Page {page.Pagination.CurrentPage}/{page.Pagination.TotalPages}");
        }

        if (page.AutoScroll)
        {
            statusParts.Add($"[{theme.Colors.Success}]Auto-scroll ON[/]");
        }

        console.MarkupLine($"[{theme.Colors.Muted}]{string.Join(" | ", statusParts)}[/]");
        console.WriteLine();
    }

    private static Color GetLevelColor(ILogLevel level, IMenuTheme theme)
    {
        return level.Name switch
        {
            "Critical" => theme.Colors.Error,
            "Error" => theme.Colors.Error,
            "Warning" => theme.Colors.Warning,
            "Information" => theme.Colors.Info,
            "Debug" => theme.Colors.Muted,
            "Trace" => theme.Colors.Muted,
            _ => theme.Colors.Foreground
        };
    }

    private static string GetLevelIcon(ILogLevel level, IMenuTheme theme)
    {
        return level.Name switch
        {
            "Critical" => "💥",
            "Error" => theme.Icons.ErrorIcon,
            "Warning" => theme.Icons.WarningIcon,
            "Information" => theme.Icons.InfoIcon,
            "Debug" => "🔍",
            "Trace" => "📝",
            _ => "·"
        };
    }

    // MA0051: Method length acceptable - procedural action menu with level/search/filter options and switch-based dispatch
#pragma warning disable MA0051 // Method is too long
    private static LogViewerPageResult PromptAction(ILogViewerPageModel page, IAnsiConsole console, IMenuTheme theme)
#pragma warning restore MA0051
    {
        var choices = new List<(string Id, string Label)>
        {
            ("level", $"[l] Change Level (current: {page.MinimumLevel})"),
            ("search", "[/] Search"),
            ("source", "[s] Filter Source"),
            ("clear", "[c] Clear Filters"),
            ("toggle_stream", page.IsStreaming ? "[p] Pause" : "[p] Resume"),
            ("toggle_scroll", page.AutoScroll ? "[a] Disable Auto-scroll" : "[a] Enable Auto-scroll"),
        };

        if (page.Pagination != null)
        {
            if (page.Pagination.HasPreviousPage)
            {
                choices.Add(("prev", "[[] Previous Page"));
            }

            if (page.Pagination.HasNextPage)
            {
                choices.Add(("next", "[]] Next Page"));
            }
        }

        choices.Add(("refresh", "[r] Refresh"));
        choices.Add(("back", "[q] Back"));

        var prompt = new SelectionPrompt<(string Id, string Label)>()
            .Title($"[{theme.Colors.Primary}]Select action:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);

        switch (selected.Id)
        {
            case "back":
                return new LogViewerPageResult { ShouldExit = true };

            case "level":
                return PromptLevelChange(page, console, theme);

            case "search":
                var searchPrompt = new TextPrompt<string>($"[{theme.Colors.Primary}]Search:[/]")
                    .AllowEmpty();
                page.SearchText = console.Prompt(searchPrompt);
                return new LogViewerPageResult { ShouldExit = false };

            case "source":
                return PromptSourceFilter(page, console, theme);

            case "clear":
                page.SearchText = null;
                page.SourceFilter = null;
                page.StartTime = null;
                page.EndTime = null;
                page.MinimumLevel = LogLevels.Information;
                return new LogViewerPageResult { ShouldExit = false };

            case "toggle_stream":
                return new LogViewerPageResult { ShouldExit = false, ToggleStreaming = true };

            case "toggle_scroll":
                page.AutoScroll = !page.AutoScroll;
                return new LogViewerPageResult { ShouldExit = false };

            case "prev":
                if (page.Pagination != null)
                {
                    page.Pagination.CurrentPage--;
                }
                return new LogViewerPageResult { ShouldExit = false };

            case "next":
                if (page.Pagination != null)
                {
                    page.Pagination.CurrentPage++;
                }
                return new LogViewerPageResult { ShouldExit = false };

            case "refresh":
                return new LogViewerPageResult { ShouldExit = false, ShouldRefresh = true };

            default:
                return new LogViewerPageResult { ShouldExit = false };
        }
    }

    private static LogViewerPageResult PromptLevelChange(ILogViewerPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var levels = LogLevels.All();

        var prompt = new SelectionPrompt<ILogLevel>()
            .Title($"[{theme.Colors.Primary}]Select minimum log level:[/]")
            .AddChoices(levels)
            .UseConverter(l => l.Name)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);
        page.MinimumLevel = selected;

        return new LogViewerPageResult { ShouldExit = false };
    }

    private static LogViewerPageResult PromptSourceFilter(ILogViewerPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var sources = new List<(string Value, string Label)>
        {
            ("", "(All Sources)")
        };

        foreach (var source in page.AvailableSources)
        {
            sources.Add((source, source));
        }

        var prompt = new SelectionPrompt<(string Value, string Label)>()
            .Title($"[{theme.Colors.Primary}]Select source filter:[/]")
            .AddChoices(sources)
            .UseConverter(s => s.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);
        page.SourceFilter = string.IsNullOrEmpty(selected.Value) ? null : selected.Value;

        return new LogViewerPageResult { ShouldExit = false };
    }
}