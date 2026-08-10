using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// A page declaration with no behaviour of its own — the shape almost every page uses.
/// </summary>
// Why: a concrete PageBase so a page type declares its pages inline as constructor arguments instead of
// each page needing its own class. A page that DOES need behaviour (a computed access rule, a conditional
// nav entry) derives from PageBase directly.
public sealed class Page : PageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Page"/> class.
    /// </summary>
    /// <param name="name">The stable name for this page, unique within its owning page type.</param>
    /// <param name="component">The component type that renders the page.</param>
    /// <param name="navItem">The sidebar entry that opens it, or <see cref="NavItem.Empty"/> to keep it out of navigation.</param>
    /// <param name="access">The rule deciding who may reach it — a <see cref="PageAccess"/> form.</param>
    public Page(string name, Type component, INavItem navItem, IPageAccess access)
        : base(name, component, navItem, access) { }
}
