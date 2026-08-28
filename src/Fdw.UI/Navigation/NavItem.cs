using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// A sidebar entry declared by the page it opens.
/// </summary>
public sealed class NavItem : INavItem
{
    /// <summary>
    /// The empty entry, declared by a page that is routable but carries no sidebar entry.
    /// </summary>
    public static INavItem Empty { get; } = new NavItem();

    private NavItem()
    {
        Label = string.Empty;
        Icon = string.Empty;
        Section = null;
        Order = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavItem"/> class.
    /// </summary>
    /// <param name="label">The display label.</param>
    /// <param name="icon">The renderer-agnostic icon name.</param>
    /// <param name="section">The owning section, or null to sit above all titled sections.</param>
    /// <param name="order">The sort order within the section.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="label"/> or <paramref name="icon"/> is null or empty.</exception>
    public NavItem(string label, string icon, INavSection? section, int order)
    {
        if (string.IsNullOrEmpty(label))
            throw new ArgumentException("A nav item requires a label.", nameof(label));
        if (string.IsNullOrEmpty(icon))
            throw new ArgumentException("A nav item requires an icon name.", nameof(icon));

        Label = label;
        Icon = icon;
        Section = section;
        Order = order;
    }

    /// <inheritdoc />
    public string Label { get; }

    /// <inheritdoc />
    public string Icon { get; }

    /// <inheritdoc />
    public INavSection? Section { get; }

    /// <inheritdoc />
    public string? SectionName => Section is null ? null : Section.Title;

    /// <inheritdoc />
    public int SectionOrder => Section is null ? 0 : Section.Order;

    /// <inheritdoc />
    public int Order { get; }
}
