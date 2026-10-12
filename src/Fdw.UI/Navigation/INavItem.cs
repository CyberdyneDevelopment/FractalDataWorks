namespace Fdw.UI.Navigation;

/// <summary>
/// The sidebar entry for a single <see cref="IPage"/>.
/// </summary>
public interface INavItem
{
    /// <summary>
    /// Gets the display label for this entry (e.g., "Connections").
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the renderer-agnostic icon name (e.g., "database"). Consumers map it to their icon set.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets the section this entry belongs to, or null to sit above all titled sections.
    /// </summary>
    INavSection? Section { get; }

    /// <summary>
    /// Gets the title of the section this entry belongs to, or null when it sits above all titled sections.
    /// </summary>
    string? SectionName { get; }

    /// <summary>
    /// Gets the sort order of this entry's SECTION relative to other sections. Lower values appear first.
    /// </summary>
    int SectionOrder { get; }

    /// <summary>
    /// Gets the sort order of this entry within its section. Lower values appear first.
    /// </summary>
    int Order { get; }
}
