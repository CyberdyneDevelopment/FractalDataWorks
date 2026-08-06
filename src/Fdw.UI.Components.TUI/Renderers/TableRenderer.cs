using System;
using System.Collections.Generic;
using Spectre.Console;

#pragma warning disable MA0016 // Prefer using collection abstraction instead of implementation - Need concrete List for rendering

namespace Fdw.UI.Components.TUI.Renderers;

/// <summary>
/// Renders collections as formatted tables.
/// </summary>
public static class TableRenderer
{
    /// <summary>
    /// Renders a collection as a formatted table.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    /// <param name="console">The console to render to</param>
    /// <param name="items">The items to render</param>
    /// <param name="columns">Dictionary of column name to value extractor functions</param>
    /// <param name="theme">Theme configuration</param>
    public static void RenderTable<T>(
        IAnsiConsole console,
        IEnumerable<T> items,
        Dictionary<string, Func<T, string>> columns,
        TUIThemeConfiguration? theme = null)
    {
        var border = theme?.Borders.Table ?? TableBorder.Rounded;
        var borderColor = theme?.Colors.Primary ?? Color.Blue;

        var table = new Table()
            .Border(border)
            .BorderColor(borderColor);

        // Add columns
        foreach (var columnName in columns.Keys)
        {
            table.AddColumn($"[bold]{columnName}[/]");
        }

        // Add rows
        foreach (var item in items)
        {
            List<string> cellValues = [];
            foreach (var columnFunc in columns.Values)
            {
                cellValues.Add(columnFunc(item));
            }
            table.AddRow(cellValues.ToArray());
        }

        console.Write(table);
    }
}
