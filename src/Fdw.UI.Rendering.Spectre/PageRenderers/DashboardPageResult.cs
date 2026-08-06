using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Result from rendering a dashboard page.
/// </summary>
public sealed class DashboardPageResult
{
    /// <summary>
    /// Gets or sets whether the page should exit.
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// Gets or sets whether the dashboard should refresh.
    /// </summary>
    public bool ShouldRefresh { get; set; }

    /// <summary>
    /// Gets or sets the selected action, if any.
    /// </summary>
    public IPageAction? Action { get; set; }

    /// <summary>
    /// Gets or sets the navigation target if a widget was selected.
    /// </summary>
    public string? NavigationTarget { get; set; }
}