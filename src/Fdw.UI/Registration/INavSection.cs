using Fdw.Collections;

namespace Fdw.UI.Registration;

/// <summary>
/// A titled group of sidebar entries.
/// </summary>
/// <remarks>
/// Why sections are a TypeCollection rather than a string on each entry: a section previously had no
/// declaration of its own — it was whatever the declared pages happened to name — so every page restated
/// its section's title AND that section's position, and nothing could reconcile two pages that disagreed.
/// They did disagree: "Administration" and "Quality" both claimed order 50, leaving their relative
/// placement undefined. A section now declares itself once and pages reference it, so disagreement is not
/// expressible and a downstream package can contribute a section exactly as it contributes a page.
/// </remarks>
public interface INavSection : ITypeOption<int, NavSectionBase>
{
    /// <summary>
    /// Gets the display title for this section (e.g., "Data Sources").
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the sort order of this section relative to other sections. Lower values appear first.
    /// </summary>
    int Order { get; }
}
