using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// A sidebar entry declared by the page it opens.
/// </summary>
// Why: values arrive through the constructor rather than as settable/overridable properties, matching how
// every other FDW option carries its values. No parameter has a default — a page author states the
// section and order explicitly rather than inheriting one silently.
public sealed class NavItem : INavItem
{
    /// <summary>
    /// The empty entry, declared by a page that is routable but carries no sidebar entry.
    /// </summary>
    // Why: a page with no sidebar entry declares THIS rather than null, so IPage.NavItem is never a
    // nullable reference and no consumer needs a null guard — it compares against Empty, the same way a
    // TypeCollection lookup is compared against its NotFound member.
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
    // Why the section arrives as a declared option rather than a name plus an order: the section owns its
    // own title and position, so a page cannot state one that disagrees with another page's.
    public NavItem(string label, string icon, INavSection? section, int order)
    {
        // Why: a nav entry with no label or no icon renders as an invisible or broken row rather than
        // failing, so both are rejected at construction instead of being substituted.
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
    // Why an explicit branch rather than `Section?.Order ?? 0`: a null Section is the declared
    // "sits above all titled sections" state, and 0 is that state's real position — not a stand-in
    // for a value that failed to arrive.
    public int SectionOrder => Section is null ? 0 : Section.Order;

    /// <inheritdoc />
    public int Order { get; }
}
