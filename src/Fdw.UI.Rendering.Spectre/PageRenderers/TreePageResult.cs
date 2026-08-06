using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Result from rendering a tree page.
/// </summary>
public sealed class TreePageResult
{
    /// <summary>
    /// Gets or sets whether the page should exit.
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// Gets or sets the selected action, if any.
    /// </summary>
    public IPageAction? Action { get; set; }

    /// <summary>
    /// Gets or sets the selected node when action was invoked.
    /// </summary>
    public ITreeNode? SelectedNode { get; set; }
}