using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Themes;
using Spectre.Console;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Renders tree pages using Spectre.Console with expandable hierarchy.
/// </summary>
public sealed class TreePageRenderer
{
    /// <summary>
    /// Renders a tree page and returns the selected action.
    /// </summary>
    public static TreePageResult Render(ITreePageModel page, SpectreRenderContext context)
    {
        var console = context.Console;
        var theme = context.Theme;

        console.Clear();

        // Render header
        RenderHeader(page, console, theme);

        // Render search bar if active
        if (!string.IsNullOrEmpty(page.SearchText))
        {
            console.MarkupLine($"[{theme.Colors.Muted}]Search: \"{page.SearchText}\"[/]");
            console.WriteLine();
        }

        // Render tree
        RenderTree(page, console, theme);

        // Render selected node details
        if (page.SelectedNode != null)
        {
            RenderNodeDetails(page.SelectedNode, console, theme);
        }

        // Render shortcuts
        RenderShortcuts(page, console, theme);

        // Prompt for action
        return PromptAction(page, console, theme);
    }

    private static void RenderHeader(ITreePageModel page, IAnsiConsole console, IMenuTheme theme)
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

    private static void RenderTree(ITreePageModel page, IAnsiConsole console, IMenuTheme theme)
    {

        if (page.RootNodes.Count == 0)
        {
            console.MarkupLine($"[{theme.Colors.Muted}]No items.[/]");
            console.WriteLine();
            return;
        }

        // Build Spectre.Console tree
        var spectreTree = new Tree($"[{theme.Colors.Secondary}]Root[/]")
        {
            Style = new Style(theme.Colors.Muted)
        };

        foreach (var rootNode in page.RootNodes)
        {
            var treeNode = spectreTree.AddNode(CreateNodeLabel(rootNode, page.SelectedNode, theme));
            AddChildNodes(treeNode, rootNode, page.SelectedNode, theme);
        }

        console.Write(spectreTree);
        console.WriteLine();
    }

    private static void AddChildNodes(TreeNode parentTreeNode, ITreeNode node, ITreeNode? selectedNode, IMenuTheme theme)
    {
        if (!node.IsExpanded || !node.HasChildren)
        {
            return;
        }

        foreach (var child in node.Children)
        {
            var childTreeNode = parentTreeNode.AddNode(CreateNodeLabel(child, selectedNode, theme));
            AddChildNodes(childTreeNode, child, selectedNode, theme);
        }
    }

    private static string CreateNodeLabel(ITreeNode node, ITreeNode? selectedNode, IMenuTheme theme)
    {
        var isSelected = string.Equals(selectedNode?.Id, node.Id, StringComparison.Ordinal);
        var expandIcon = node.HasChildren
            ? (node.IsExpanded ? theme.Icons.ExpandedIcon : theme.Icons.CollapsedIcon)
            : " ";

        var statusColor = GetStatusColor(node.Status, theme);
        var labelColor = isSelected ? theme.Colors.Selected : theme.Colors.Foreground;
        var icon = node.Icon ?? GetDefaultIcon(node.NodeType);

        var selectionMarker = isSelected ? $"[{theme.Colors.Primary}]►[/] " : "  ";

        return $"{selectionMarker}[{theme.Colors.Muted}]{expandIcon}[/] [{statusColor}]{icon}[/] [{labelColor}]{Markup.Escape(node.Label)}[/] [{theme.Colors.Muted}]({node.NodeType})[/]";
    }

    private static Color GetStatusColor(IRowStatus status, IMenuTheme theme)
    {
        return status.Name switch
        {
            "Success" => theme.Colors.Success,
            "Warning" => theme.Colors.Warning,
            "Error" => theme.Colors.Error,
            "Disabled" => theme.Colors.Muted,
            _ => theme.Colors.Foreground
        };
    }

    private static string GetDefaultIcon(string nodeType)
    {
        return nodeType.ToLowerInvariant() switch
        {
            "dataset" => "📊",
            "field" => "📄",
            "mapping" => "🔗",
            "source" => "💾",
            "join" => "⊕",
            "pipeline" => "⚙",
            "stage" => "▶",
            "workflow" => "🔄",
            "step" => "•",
            "folder" => "📁",
            "file" => "📄",
            "connection" => "🔌",
            _ => "○"
        };
    }

    private static void RenderNodeDetails(ITreeNode node, IAnsiConsole console, IMenuTheme theme)
    {
        console.MarkupLine($"[{theme.Colors.Secondary} bold]Selected: {Markup.Escape(node.Label)}[/]");

        if (node.Metadata != null && node.Metadata.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.Simple)
                .BorderColor(theme.Colors.Muted)
                .HideHeaders();

            table.AddColumn("Property");
            table.AddColumn("Value");

            foreach (var (key, value) in node.Metadata.Take(5))
            {
                table.AddRow(
                    $"[{theme.Colors.Muted}]{key}[/]",
                    $"[{theme.Colors.Foreground}]{value ?? "-"}[/]"
                );
            }

            if (node.Metadata.Count > 5)
            {
                table.AddRow($"[{theme.Colors.Muted}]...[/]", $"[{theme.Colors.Muted}]+{node.Metadata.Count - 5} more[/]");
            }

            console.Write(table);
        }

        console.WriteLine();
    }

    private static void RenderShortcuts(ITreePageModel page, IAnsiConsole console, IMenuTheme theme)
    {
        var shortcuts = new List<string>
        {
            $"[{theme.Colors.Primary}]↑↓[/]=Navigate",
            $"[{theme.Colors.Primary}]Enter[/]=Select",
            $"[{theme.Colors.Primary}]←→[/]=Collapse/Expand",
            $"[{theme.Colors.Primary}]/[/]=Search"
        };

        foreach (var action in page.TreeActions.Where(a => a.IsEnabled && a.Shortcut.HasValue))
        {
            shortcuts.Add($"[{theme.Colors.Primary}]{action.Shortcut}[/]={action.Label}");
        }

        shortcuts.Add($"[{theme.Colors.Primary}]q[/]=Back");

        console.MarkupLine($"[{theme.Colors.Muted}]{string.Join("  ", shortcuts)}[/]");
        console.WriteLine();
    }

    // MA0051: Method length acceptable - procedural tree navigation with node flattening, expand/collapse, and action dispatch
#pragma warning disable MA0051 // Method is too long
    private static TreePageResult PromptAction(ITreePageModel page, IAnsiConsole console, IMenuTheme theme)
#pragma warning restore MA0051
    {
        var choices = new List<(string Id, string Label, object? Data)>();

        // Node navigation - flatten tree for selection
        var flatNodes = FlattenTree(page.RootNodes).ToList();
        foreach (var node in flatNodes.Where(n => n.IsSelectable))
        {
            var indent = new string(' ', node.Depth * 2);
            var icon = node.Icon ?? GetDefaultIcon(node.NodeType);
            var isSelected = string.Equals(page.SelectedNode?.Id, node.Id, StringComparison.Ordinal) ? "►" : " ";
            choices.Add(($"select:{node.Id}", $"{isSelected}{indent}{icon} {node.Label}", node));
        }

        // Add separator
        choices.Add(("---", "────────────", null));

        // Tree actions
        foreach (var action in page.TreeActions.Where(a => a.IsEnabled))
        {
            var shortcut = action.Shortcut.HasValue ? $"[{action.Shortcut}] " : "";
            choices.Add((action.Id, $"{shortcut}{action.Label}", null));
        }

        // Node actions (if node selected)
        if (page.SelectedNode != null)
        {
            foreach (var action in page.NodeActions.Where(a => a.IsEnabled))
            {
                var shortcut = action.Shortcut.HasValue ? $"[{action.Shortcut}] " : "";
                var label = action.IsDestructive
                    ? $"[{theme.Colors.Error}]{shortcut}{action.Label}[/]"
                    : $"{shortcut}{action.Label}";
                choices.Add((action.Id, label, null));
            }
        }

        // Standard actions
        choices.Add(("expand_all", "[e] Expand All", null));
        choices.Add(("collapse_all", "[c] Collapse All", null));
        choices.Add(("search", "[/] Search", null));
        choices.Add(("back", "[q] Back", null));

        // Filter out separator for prompt
        var filteredChoices = choices.Where(c => !string.Equals(c.Id, "---", StringComparison.Ordinal)).ToList();

        var prompt = new SelectionPrompt<(string Id, string Label, object? Data)>()
            .Title($"[{theme.Colors.Primary}]Select node or action:[/]")
            .AddChoices(filteredChoices)
            .UseConverter(c => c.Label)
            .HighlightStyle(new Style(theme.Colors.Selected));

        var selected = console.Prompt(prompt);

        // Handle node selection
        if (selected.Id.StartsWith("select:", StringComparison.Ordinal))
        {
            var nodeId = selected.Id.Substring(7);
            var node = flatNodes.FirstOrDefault(n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
            if (node != null)
            {
                page.SelectedNode = node;
            }
            return new TreePageResult { ShouldExit = false };
        }

        switch (selected.Id)
        {
            case "back":
                return new TreePageResult { ShouldExit = true };

            case "expand_all":
                ExpandAll(page.RootNodes, true);
                return new TreePageResult { ShouldExit = false };

            case "collapse_all":
                ExpandAll(page.RootNodes, false);
                return new TreePageResult { ShouldExit = false };

            case "search":
                var searchPrompt = new TextPrompt<string>($"[{theme.Colors.Primary}]Search:[/]")
                    .AllowEmpty();
                page.SearchText = console.Prompt(searchPrompt);
                return new TreePageResult { ShouldExit = false };

            default:
                var action = page.TreeActions.FirstOrDefault(a => string.Equals(a.Id, selected.Id, StringComparison.Ordinal))
                    ?? page.NodeActions.FirstOrDefault(a => string.Equals(a.Id, selected.Id, StringComparison.Ordinal));

                if (action != null)
                {
                    if (action.RequiresConfirmation)
                    {
                        var confirmed = console.Confirm(
                            $"[{theme.Colors.Warning}]Are you sure you want to {action.Label.ToLowerInvariant()}?[/]",
                            false);

                        if (!confirmed)
                        {
                            return new TreePageResult { ShouldExit = false };
                        }
                    }

                    return new TreePageResult
                    {
                        ShouldExit = true,
                        Action = action,
                        SelectedNode = page.SelectedNode
                    };
                }

                return new TreePageResult { ShouldExit = false };
        }
    }

    private static IEnumerable<ITreeNode> FlattenTree(IEnumerable<ITreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node.IsExpanded && node.HasChildren)
            {
                foreach (var child in FlattenTree(node.Children))
                {
                    yield return child;
                }
            }
        }
    }

    private static void ExpandAll(IEnumerable<ITreeNode> nodes, bool expand)
    {
        foreach (var node in nodes)
        {
            node.IsExpanded = expand;
            if (node.HasChildren)
            {
                ExpandAll(node.Children, expand);
            }
        }
    }
}