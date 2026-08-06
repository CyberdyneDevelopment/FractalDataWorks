using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// Result from rendering a detail page.
/// </summary>
public sealed class DetailPageResult
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
    /// Gets or sets the component to edit, if user chose to edit a field.
    /// </summary>
    public IComponentModel? EditComponent { get; set; }
}