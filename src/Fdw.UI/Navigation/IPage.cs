using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// One routable page declared by a <see cref="PageTypes"/> option, together with the sidebar entry
/// that opens it.
/// </summary>
// Why: a page and its nav entry are ONE declaration. The previous shape registered an assembly plus a
// SEPARATE flat list of nav descriptors, so the two could disagree silently — the sidebar carried a
// /configuration/issues link to a page that existed nowhere, and eight real pages had no entry at all.
// Neither is expressible here: a nav entry hangs off the page it opens, and a page with no entry says so
// with an explicit NavItem.Empty.
//
// Why no Route property: the route already exists on the component as one or more [Route] attributes
// (what @page compiles to). Declaring it again here would be a second copy free to drift. The RENDERER
// resolves the address from Component — the Blazor renderer reflects RouteAttribute.Template, a
// terminal renderer resolves its own addressing — which also keeps this assembly free of any Blazor
// dependency so a non-Blazor renderer can consume the same declarations.
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
    // Why declared beside the component it guards: the declaration that adds a page also states who may
    // reach it, so a package cannot contribute a page while leaving that question to whoever wires routing.
    //
    // What this actually drives, precisely: NavTree.Build is the ONLY consumer in either repo, and it
    // filters the SIDEBAR. Nothing evaluates this at the route — a page withheld from navigation is still
    // reachable by typing its URL unless its component guards itself. Closing that gap needs router
    // integration in Fdw.UI.Rendering.Blazor and is tracked as FDW-647; until then this is a visibility
    // rule, not an authorization boundary, and must not be read as one.
    IPageAccess Access { get; }
}
