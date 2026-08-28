using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// One routable page declared by a <see cref="PageTypes"/> option, together with the sidebar entry
/// that opens it.
/// </summary>
public interface IPage
{
    /// <summary>
    /// Gets the stable name for this page, unique within its owning page type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the component type that renders this page. The renderer resolves the page's address from it.
    /// </summary>
    Type Component { get; }

    /// <summary>
    /// Gets the sidebar entry that opens this page, or <see cref="NavItem.Empty"/> when the page is
    /// routable but deliberately absent from navigation (e.g. a detail page reached by drill-down).
    /// </summary>
    INavItem NavItem { get; }

    /// <summary>
    /// Gets the rule deciding who may reach this page — <see cref="PageAccess.Anonymous"/>,
    /// <see cref="PageAccess.Authenticated"/>, or <see cref="PageAccess.RequiringPermission"/>.
    /// </summary>
    IPageAccess Access { get; }
}
