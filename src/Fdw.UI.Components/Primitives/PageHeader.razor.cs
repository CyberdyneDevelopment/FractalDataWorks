using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// The band at the top of a console page: breadcrumb eyebrow, title, optional subtitle, and the
/// page's actions pushed to the right.
/// </summary>
/// <remarks>
/// <para>
/// Every page in Fdw.UI.Pages opened with the same nine lines of nested divs. The nesting is what
/// the <c>pagehead</c> rules select on — <c>.pagehead .ey</c>, <c>.pagehead h1</c>,
/// <c>.pagehead .sub</c> — so a page that spelled it wrong lost its type scale silently. Naming the
/// shape is what makes that unspellable.
/// </para>
/// <para>
/// The text parameters are strings, and Razor html-encodes them on the way out: write the character
/// (<c>&amp;</c>, <c>›</c>), not the entity, or the entity itself is what renders.
/// <see cref="SubtitleContent"/> is there for the handful of headers whose subtitle carries markup.
/// </para>
/// </remarks>
public partial class PageHeader
{
    /// <summary>
    /// Gets or sets the small uppercase line above the title — where the page sits in the console.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Eyebrow { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sentence under the title.
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets css classes appended to the subtitle, for the headers that render it monospaced.
    /// </summary>
    [Parameter]
    public string? SubtitleClass { get; set; }

    /// <summary>
    /// Gets or sets subtitle markup, for the headers whose subtitle is more than a sentence.
    /// Rendered after <see cref="Subtitle"/>, and independent of it.
    /// </summary>
    [Parameter]
    public RenderFragment? SubtitleContent { get; set; }

    /// <summary>
    /// Gets or sets the buttons and links on the right of the band.
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Gets or sets css classes appended to the actions row, for the headers that align it
    /// differently.
    /// </summary>
    [Parameter]
    public string? ActionsClass { get; set; }

    /// <summary>
    /// Gets or sets css classes appended to the band itself.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private string HeaderClass => Join("pagehead", Class);

    private string SubtitleClassName => Join("sub", SubtitleClass);

    private string ActionsClassName => Join("actions", ActionsClass);

    private static string Join(string baseClass, string? extra)
        => string.IsNullOrEmpty(extra) ? baseClass : baseClass + " " + extra;
}
