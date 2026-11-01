using System.Collections.Generic;

namespace Fdw.UI.Navigation;

/// <summary>
/// One rendered sidebar section: its label and the pages in it, already ordered.
/// </summary>
public sealed class NavGroup
{
    internal NavGroup(string? label, int order, IReadOnlyList<IPage> pages)
    {
        Label = label;
        Order = order;
        Pages = pages;
    }

    /// <summary>
    /// Gets the section label, or null for the unlabelled block that renders above titled sections.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Gets this section's position relative to other sections.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the pages in this section, ordered by their nav entry's Order.
    /// </summary>
    public IReadOnlyList<IPage> Pages { get; }
}
