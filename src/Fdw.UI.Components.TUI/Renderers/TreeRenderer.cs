using System;
using Spectre.Console;

namespace Fdw.UI.Components.TUI.Renderers;

/// <summary>
/// Renders hierarchical data as trees.
/// </summary>
public static class TreeRenderer
{
    /// <summary>
    /// Renders hierarchical data as a tree.
    /// </summary>
    /// <param name="console">The console to render to</param>
    /// <param name="rootLabel">The root node label</param>
    /// <param name="buildTree">Action to build the tree structure</param>
    /// <param name="theme">Theme configuration</param>
    public static void RenderTree(
        IAnsiConsole console,
        string rootLabel,
        Action<IHasTreeNodes> buildTree,
        TUIThemeConfiguration? theme = null)
    {
        var tree = new Tree(rootLabel);
        buildTree(tree);
        console.Write(tree);
    }
}
