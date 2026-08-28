using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Themes;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Renders an <see cref="IBrowsePageModel"/> as a Miller-column layout and prompts
/// for the next action. The renderer shows a window of columns (3 by default)
/// centred on the active one; the caller re-renders after each action and is
/// responsible for populating the next column on drill.
/// </summary>
public static class BrowsePageRenderer
{
    /// <summary>Default number of columns visible at once in the layout.</summary>
    public const int DefaultVisibleColumns = 3;

    /// <summary>
    /// Renders the supplied browse page and prompts for an action. Returns a
    /// <see cref="BrowsePageResult"/> describing what the user chose.
    /// </summary>
    public static BrowsePageResult Render(
        IBrowsePageModel page,
        SpectreRenderContext context,
        int visibleColumns = DefaultVisibleColumns)
    {
        if (page is null) throw new ArgumentNullException(nameof(page));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (visibleColumns < 1) visibleColumns = 1;

        var console = context.Console;
        var theme = context.Theme;

        console.Clear();
        RenderHeader(page, console, theme);
        RenderBreadcrumb(page, console, theme);
        RenderColumns(page, console, theme, visibleColumns);

        if (page.PreviewRows is not null)
            RenderPreview(page.PreviewRows, console, theme);

        return PromptAction(page, console, theme);
    }

    private static void RenderHeader(IBrowsePageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var title = new Markup($"[{theme.Colors.Primary} bold]{page.Title}[/]");
        console.Write(title);
        console.WriteLine();
        if (!string.IsNullOrEmpty(page.Description))
            console.MarkupLine($"[{theme.Colors.Muted}]{page.Description}[/]");
    }

    private static void RenderBreadcrumb(IBrowsePageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        if (page.Breadcrumb.Count == 0)
            return;

        var crumbs = string.Join(
            $" [{theme.Colors.Muted}]›[/] ",
            page.Breadcrumb.Select(c => $"[{theme.Colors.Secondary}]{c}[/]"));
        console.MarkupLine(crumbs);
        console.WriteLine();
    }

    private static void RenderColumns(
        IBrowsePageModel page,
        IAnsiConsole console,
        IMenuTheme theme,
        int visibleColumns)
    {
        var columns = page.Columns;
        if (columns.Count == 0)
        {
            console.MarkupLine($"[{theme.Colors.Muted}](no columns)[/]");
            return;
        }

        var active = Math.Clamp(page.ActiveColumnIndex, 0, columns.Count - 1);

        var end = active + 1;
        var start = Math.Max(0, end - visibleColumns);
        var window = columns.Skip(start).Take(end - start).ToList();

        var grid = new Grid();
        for (int i = 0; i < window.Count; i++)
            grid.AddColumn(new GridColumn().NoWrap());

        grid.AddRow(window.Select((col, i) =>
            (IRenderable)BuildColumnPanel(col, theme, isActive: (start + i) == active)).ToArray());

        console.Write(grid);
        console.WriteLine();
    }

    private static Panel BuildColumnPanel(IBrowseColumnModel column, IMenuTheme theme, bool isActive)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn(string.Empty));

        if (column.IsLoading)
        {
            table.AddRow(new Markup($"[{theme.Colors.Muted}]... loading[/]"));
        }
        else if (column.Items.Count == 0)
        {
            table.AddRow(new Markup($"[{theme.Colors.Muted}](empty)[/]"));
        }
        else
        {
            for (var i = 0; i < column.Items.Count; i++)
            {
                var item = column.Items[i];
                var selected = i == column.SelectedIndex;
                table.AddRow(new Markup(FormatItem(item, theme, isActive && selected)));
            }
        }

        var borderColor = isActive ? theme.Colors.Primary : theme.Colors.InputBorder;
        return new Panel(table)
        {
            Header = new PanelHeader($"[{(isActive ? theme.Colors.Primary : theme.Colors.Muted)} bold]{column.Title}[/]"),
            Border = theme.Borders.Panel,
            BorderStyle = new Style(borderColor),
            Padding = new Padding(0, 0),
        };
    }

    private static string FormatItem(IBrowseItem item, IMenuTheme theme, bool selected)
    {
        var prefix = selected ? theme.Icons.SelectedIndicator : " ";
        var label = selected
            ? $"[{theme.Colors.Selected}]{item.Label}[/]"
            : $"[{theme.Colors.Foreground}]{item.Label}[/]";

        var detail = string.IsNullOrEmpty(item.Detail)
            ? string.Empty
            : $" [{theme.Colors.Muted}]{item.Detail}[/]";

        var chevron = item.HasChildren ? $" [{theme.Colors.Muted}]›[/]" : string.Empty;
        return $"{prefix} {label}{detail}{chevron}";
    }

    private static void RenderPreview(IListPageModel preview, IAnsiConsole console, IMenuTheme theme)
    {
        console.MarkupLine($"[{theme.Colors.Secondary} bold]{preview.Title}[/]");
        if (preview.Rows.Count == 0)
        {
            console.MarkupLine($"[{theme.Colors.Muted}](no rows)[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        foreach (var col in preview.Columns)
            table.AddColumn(new TableColumn($"[{theme.Colors.Secondary}]{col.Header}[/]"));

        foreach (var row in preview.Rows)
        {
            var cells = preview.Columns
                .Select(col => row.Values.TryGetValue(col.Id, out var v)
                    ? (IRenderable)new Markup(Markup.Escape(v?.ToString() ?? string.Empty))
                    : (IRenderable)new Markup(string.Empty))
                .ToArray();
            table.AddRow(cells);
        }

        console.Write(table);
    }

    private static BrowsePageResult PromptAction(IBrowsePageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var active = Math.Clamp(page.ActiveColumnIndex, 0, Math.Max(0, page.Columns.Count - 1));
        var activeColumn = page.Columns.Count > 0 ? page.Columns[active] : null;

        var choices = new List<string>();
        if (activeColumn is not null && activeColumn.Items.Count > 0)
        {
            foreach (var item in activeColumn.Items)
                choices.Add(item.Label);
        }
        choices.Add("← Back");
        choices.Add("Refresh");
        choices.Add("Quit");

        var selection = console.Prompt(
            new SelectionPrompt<string>()
                .Title($"[{theme.Colors.Primary}]Select or action:[/]")
                .PageSize(Math.Max(10, choices.Count))
                .AddChoices(choices)
                .HighlightStyle(new Style(theme.Colors.Selected)));

        return selection switch
        {
            "Quit" => new BrowsePageResult(BrowseAction.Quit, -1, null),
            "Refresh" => new BrowsePageResult(BrowseAction.Refresh, -1, null),
            "← Back" => new BrowsePageResult(BrowseAction.Back, -1, null),
            _ => ResolveItemSelection(activeColumn, selection),
        };
    }

    private static BrowsePageResult ResolveItemSelection(IBrowseColumnModel? column, string label)
    {
        if (column is null)
            return new BrowsePageResult(BrowseAction.Quit, -1, null);

        for (var i = 0; i < column.Items.Count; i++)
        {
            if (string.Equals(column.Items[i].Label, label, StringComparison.Ordinal))
                return new BrowsePageResult(BrowseAction.DrillDown, i, column.Items[i].Payload);
        }

        return new BrowsePageResult(BrowseAction.Quit, -1, null);
    }
}
