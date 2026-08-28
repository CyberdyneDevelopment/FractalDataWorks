using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Themes;
using Spectre.Console;

using IPageActionType = Fdw.UI.Abstractions.Pages.IPageAction;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Renders list pages using Spectre.Console with table, pagination, and actions.
/// </summary>
public sealed class ListPageRenderer
{
    /// <summary>
    /// Renders a list page and returns the selected action.
    /// </summary>
    public static ListPageResult Render(IListPageModel page, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        while (true)
        {
            console.Clear();

            // Render title
            RenderHeader(page, console, theme);

            // Render search bar if there's search text
            if (!string.IsNullOrEmpty(page.SearchText))
            {
                console.MarkupLine($"[{theme.Colors.Muted}]Search: {page.SearchText}[/]");
                console.WriteLine();
            }

            // Render table
            RenderTable(page, console, theme);

            // Render pagination
            RenderPagination(page.Pagination, console, theme);

            // Render shortcuts help
            RenderShortcutsHelp(page, console, theme);

            // Prompt for action
            var result = PromptAction(page, console, theme);
            if (result.ShouldExit)
            {
                return result;
            }

            // Handle internal navigation (pagination)
            if (string.Equals(result.Action?.Id, "prev_page", StringComparison.Ordinal))
            {
                page.Pagination.CurrentPage = Math.Max(1, page.Pagination.CurrentPage - 1);
            }
            else if (string.Equals(result.Action?.Id, "next_page", StringComparison.Ordinal))
            {
                page.Pagination.CurrentPage = Math.Min(page.Pagination.TotalPages, page.Pagination.CurrentPage + 1);
            }
        }
    }

    private static void RenderHeader(IListPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var rule = new Rule($"[{theme.Colors.Primary} bold]{page.Title}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(theme.Colors.Primary)
        };
        console.Write(rule);

        if (!string.IsNullOrEmpty(page.Description))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]{page.Description}[/]");
        }

        console.WriteLine();
    }

    // MA0051: Method length acceptable - procedural table rendering with column alignment, selection checkboxes, and formatting
#pragma warning disable MA0051 // Method is too long
    private static void RenderTable(IListPageModel page, IAnsiConsole console, IMenuTheme theme)
#pragma warning restore MA0051
    {
        var table = new Table()
            .Border(theme.Borders.Table)
            .BorderColor(theme.Colors.Muted);

        // Add selection column if multi-select is allowed
        if (page.AllowMultiSelect)
        {
            table.AddColumn(new TableColumn("[dim]☑[/]").Centered().Width(3));
        }

        // Add index/row number column
        table.AddColumn(new TableColumn("[dim]#[/]").Centered().Width(4));

        // Add columns from definition
        foreach (var col in page.Columns.Where(c => c.IsVisible))
        {
            var column = new TableColumn($"[{theme.Colors.Secondary}]{col.Header}[/]");

            switch (col.Alignment.Name)
            {
                case "Center":
                    column.Centered();
                    break;
                case "Right":
                    column.RightAligned();
                    break;
                default:
                    column.LeftAligned();
                    break;
            }

            if (col.Width.HasValue)
            {
                column.Width(col.Width.Value);
            }

            table.AddColumn(column);
        }

        // Add rows
        var selectedSet = new HashSet<int>(page.SelectedIndices);
        for (var i = 0; i < page.Rows.Count; i++)
        {
            var row = page.Rows[i];
            var cells = new List<string>();

            // Selection checkbox
            if (page.AllowMultiSelect)
            {
                var checkbox = selectedSet.Contains(i)
                    ? $"[{theme.Colors.Success}]{theme.Icons.CheckedIndicator}[/]"
                    : $"[{theme.Colors.Muted}]{theme.Icons.UncheckedIndicator}[/]";
                cells.Add(checkbox);
            }

            // Row number
            cells.Add($"[{theme.Colors.Muted}]{i + 1}[/]");

            // Cell values
            foreach (var col in page.Columns.Where(c => c.IsVisible))
            {
                var value = row.Values.TryGetValue(col.Id, out var v) ? v : null;
                var formatted = FormatCellValue(value, col.FormatString);
                var color = GetRowStatusColor(row.Status, theme);
                cells.Add($"[{color}]{Markup.Escape(formatted)}[/]");
            }

            table.AddRow(cells.ToArray());
        }

        // Handle empty state
        if (page.Rows.Count == 0)
        {
            var colCount = page.Columns.Count(c => c.IsVisible) + (page.AllowMultiSelect ? 2 : 1);
            var emptyCells = Enumerable.Repeat($"[{theme.Colors.Muted}]-[/]", colCount).ToArray();
            table.AddRow(emptyCells);
            console.Write(table);
            console.MarkupLine($"[{theme.Colors.Muted}]No {page.EntityTypeName} found.[/]");
        }
        else
        {
            console.Write(table);
        }

        console.WriteLine();
    }

    private static void RenderPagination(IPaginationState pagination, IAnsiConsole console, IMenuTheme theme)
    {
        if (pagination.TotalPages <= 1)
        {
            return;
        }

        var prevIndicator = pagination.HasPreviousPage
            ? $"[{theme.Colors.Primary}]← Prev[/]"
            : $"[{theme.Colors.Muted}]← Prev[/]";

        var nextIndicator = pagination.HasNextPage
            ? $"[{theme.Colors.Primary}]Next →[/]"
            : $"[{theme.Colors.Muted}]Next →[/]";

        console.MarkupLine($"  {prevIndicator}  [{theme.Colors.Foreground}]Page {pagination.CurrentPage} of {pagination.TotalPages}[/] ({pagination.TotalItems} items)  {nextIndicator}");
        console.WriteLine();
    }

    private static void RenderShortcutsHelp(IListPageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var shortcuts = new List<string>();

        foreach (var action in page.ListActions.Where(a => a.IsEnabled && a.Shortcut.HasValue))
        {
            shortcuts.Add($"[{theme.Colors.Primary}]{action.Shortcut}[/]={action.Label}");
        }

        foreach (var action in page.RowActions.Where(a => a.IsEnabled && a.Shortcut.HasValue))
        {
            shortcuts.Add($"[{theme.Colors.Primary}]{action.Shortcut}[/]={action.Label}");
        }

        // Add navigation shortcuts
        if (page.Pagination.TotalPages > 1)
        {
            shortcuts.Add($"[{theme.Colors.Primary}][[/]/[{theme.Colors.Primary}]][/]=Page");
        }

        shortcuts.Add($"[{theme.Colors.Primary}]q[/]=Back");
        shortcuts.Add($"[{theme.Colors.Primary}]/[/]=Search");

        console.MarkupLine($"[{theme.Colors.Muted}]{string.Join("  ", shortcuts)}[/]");
        console.WriteLine();
    }

    // MA0051: Method length acceptable - procedural action menu with list/row actions, multi-select, pagination, and switch-based dispatch
#pragma warning disable MA0051 // Method is too long
    private static ListPageResult PromptAction(IListPageModel page, IAnsiConsole console, IMenuTheme theme)
#pragma warning restore MA0051
    {
        var choices = new List<(string Id, string Label)>();

        // List actions first
        foreach (var listAction in page.ListActions.Where(a => a.IsEnabled))
        {
            var label = listAction.Shortcut.HasValue
                ? $"[{listAction.Shortcut}] {listAction.Label}"
                : listAction.Label;
            choices.Add((listAction.Id, label));
        }

        // Row actions (if rows exist)
        if (page.Rows.Count > 0)
        {
            foreach (var rowAction in page.RowActions.Where(a => a.IsEnabled))
            {
                var label = rowAction.Shortcut.HasValue
                    ? $"[{rowAction.Shortcut}] {rowAction.Label}"
                    : rowAction.Label;
                choices.Add((rowAction.Id, label));
            }
        }

        // Navigation
        if (page.Pagination.HasPreviousPage)
        {
            choices.Add(("prev_page", "[[] Previous Page"));
        }

        if (page.Pagination.HasNextPage)
        {
            choices.Add(("next_page", "[]] Next Page"));
        }

        choices.Add(("/", "Search"));
        choices.Add(("back", "[q] Back"));

        var prompt = new SelectionPrompt<(string Id, string Label)>()
            .Title($"[{theme.Colors.Primary}]Select action:[/]")
            .AddChoices(choices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);

        // Handle search
        if (string.Equals(selected.Id, "/", StringComparison.Ordinal))
        {
            var searchPrompt = new TextPrompt<string>($"[{theme.Colors.Primary}]Search:[/]")
                .AllowEmpty();
            page.SearchText = console.Prompt(searchPrompt);
            return ListPageResult.Continue();
        }

        // Handle back
        if (string.Equals(selected.Id, "back", StringComparison.Ordinal))
        {
            return ListPageResult.Exit();
        }

        // Handle pagination (internal)
        if (selected.Id is "prev_page" or "next_page")
        {
            var paginationAction = page.ListActions.FirstOrDefault(a => string.Equals(a.Id, selected.Id, StringComparison.Ordinal))
                ?? new PageActionStub { Id = selected.Id, Label = selected.Label };
            return ListPageResult.Continue(paginationAction);
        }

        // Find the action
        var action = page.ListActions.FirstOrDefault(a => string.Equals(a.Id, selected.Id, StringComparison.Ordinal))
            ?? page.RowActions.FirstOrDefault(a => string.Equals(a.Id, selected.Id, StringComparison.Ordinal));

        if (action == null)
        {
            return ListPageResult.Continue();
        }

        // If it's a row action, prompt for row selection
        if (page.RowActions.Any(a => string.Equals(a.Id, action.Id, StringComparison.Ordinal)) && page.Rows.Count > 0)
        {
            var rowChoices = page.Rows
                .Select((r, i) => (Index: i, Row: r))
                .Where(x => x.Row.IsSelectable)
                .ToList();

            if (rowChoices.Count == 0)
            {
                console.MarkupLine($"[{theme.Colors.Warning}]No selectable rows[/]");
                return ListPageResult.Continue();
            }

            var rowPrompt = new SelectionPrompt<(int Index, IListRowModel Row)>()
                .Title($"[{theme.Colors.Primary}]Select {page.EntityTypeName}:[/]")
                .AddChoices(rowChoices)
                .UseConverter(x => GetRowDisplayText(x.Row, page.Columns))
                .HighlightStyle(new Style(theme.Colors.Selected));

            var selectedRow = console.Prompt(rowPrompt);

            // Handle confirmation for destructive actions
            if (action.RequiresConfirmation)
            {
                var confirmed = console.Confirm(
                    $"[{theme.Colors.Warning}]Are you sure you want to {action.Label.ToLowerInvariant()}?[/]",
                    false);

                if (!confirmed)
                {
                    return ListPageResult.Continue();
                }
            }

            return ListPageResult.Selected(action, selectedRow.Row.Id, selectedRow.Index);
        }

        return ListPageResult.Selected(action);
    }

    private static string FormatCellValue(object? value, string? formatString)
    {
        if (value == null)
        {
            return "-";
        }

        if (!string.IsNullOrEmpty(formatString) && value is IFormattable formattable)
        {
            return formattable.ToString(formatString, null) ?? "-";
        }

        return value.ToString() ?? "-";
    }

    private static string GetRowStatusColor(IRowStatus status, IMenuTheme theme)
    {
        return status.Name switch
        {
            "Success" => theme.Colors.Success.ToString(),
            "Warning" => theme.Colors.Warning.ToString(),
            "Error" => theme.Colors.Error.ToString(),
            "Disabled" => theme.Colors.Muted.ToString(),
            _ => theme.Colors.Foreground.ToString()
        };
    }

    private static string GetRowDisplayText(IListRowModel row, IReadOnlyList<IListColumnDefinition> columns)
    {
        // Use first visible column value as display text
        var firstCol = columns.FirstOrDefault(c => c.IsVisible);
        if (firstCol != null && row.Values.TryGetValue(firstCol.Id, out var value))
        {
            return value?.ToString() ?? row.Id.ToString() ?? "Unknown";
        }

        return row.Id.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Stub implementation for internal actions.
    /// </summary>
    private sealed class PageActionStub : IPageActionType
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string? Icon { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsDestructive { get; set; }
        public bool RequiresConfirmation { get; set; }
        public char? Shortcut { get; set; }
    }
}