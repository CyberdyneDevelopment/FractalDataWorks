using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Result from rendering a log viewer page.
/// </summary>
public sealed class LogViewerPageResult
{
    /// <summary>
    /// Gets or sets whether the page should exit.
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// Gets or sets whether to toggle streaming mode.
    /// </summary>
    public bool ToggleStreaming { get; set; }

    /// <summary>
    /// Gets or sets whether to refresh the logs.
    /// </summary>
    public bool ShouldRefresh { get; set; }

    /// <summary>
    /// Gets or sets the selected log entry for details view.
    /// </summary>
    public ILogEntry? SelectedEntry { get; set; }
}