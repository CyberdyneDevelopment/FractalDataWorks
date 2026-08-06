using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.UI.Registration;

/// <summary>
/// Builds the ordered sidebar from declared pages, keeping only what the caller may reach.
/// </summary>
// Why: the grouping and permission filter are real logic, so they live here rather than in a renderer's
// markup — a Blazor sidebar and a terminal menu get the same tree from the same rule. It takes a page
// SEQUENCE rather than reading PageTypes itself, so a host's own pages and the registered packages' pages
// go through one identical path instead of the host special-casing its own.
public static class NavTree
{
    /// <summary>
    /// Builds the sidebar sections from the supplied pages.
    /// </summary>
    /// <param name="pages">Every page to consider, host-owned and packaged alike.</param>
    /// <param name="hasPermission">
    /// Answers whether the current caller holds a named permission. Invoked once per page that declares
    /// one; pages declaring none are always included.
    /// </param>
    /// <returns>Sections ordered by <see cref="INavItem.SectionOrder"/>, each holding its ordered pages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    public static IReadOnlyList<NavGroup> Build(IEnumerable<IPage> pages, Func<string, bool> hasPermission)
    {
        if (pages is null)
            throw new ArgumentNullException(nameof(pages));

        // Why: no permission check means every page renders — the failure mode is showing a caller links
        // they cannot use, so the predicate is required rather than defaulted to "allow".
        if (hasPermission is null)
            throw new ArgumentNullException(nameof(hasPermission));

        return pages
            // Why: compared against the Empty sentinel, not null-checked — a page with no sidebar entry
            // declares NavItem.Empty, so there is no nullable to guard here.
            .Where(p => !ReferenceEquals(p.NavItem, NavItem.Empty))
            .Where(p => p.RequiredPermission is null || hasPermission(p.RequiredPermission))
            .GroupBy(p => p.NavItem.SectionName, StringComparer.Ordinal)
            // Why: SectionOrder is carried on every entry, so the first entry in the section decides its
            // position — entries naming one section must agree, which the contract states.
            .Select(g => new NavGroup(
                g.Key,
                g.First().NavItem.SectionOrder,
                g.OrderBy(p => p.NavItem.Order).ToList()))
            .OrderBy(s => s.Order)
            .ToList();
    }
}
