using System;

namespace Fdw.UI.Registration;

/// <summary>
/// One routable page declared by a <see cref="PageTypes"/> option, together with the sidebar entry
/// that opens it.
/// </summary>
// Why: a page and its nav entry are ONE declaration. The previous shape registered an assembly plus a
// SEPARATE flat list of nav descriptors, so the two could disagree silently — the sidebar carried a
// /configuration/issues link to a page that existed nowhere, and eight real pages had no entry at all.
// Neither is expressible here: a nav entry hangs off the page it opens, and a page with no entry says so
// with an explicit null.
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
    /// Gets the permission a caller must hold to reach this page, or null when any authenticated user may.
    /// </summary>
    // Why: declared beside the component it guards so ONE declaration serves both the nav filter and the
    // route authorization check, rather than the sidebar and the router each carrying their own rule.
    string? RequiredPermission { get; }
}
