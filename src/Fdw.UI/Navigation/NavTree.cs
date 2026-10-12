using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.UI.Navigation;

/// <summary>
/// Builds the ordered sidebar from declared pages, keeping only what the caller may reach.
/// </summary>
public static class NavTree
{
    /// <summary>
    /// Builds the sidebar sections from the supplied pages.
    /// </summary>
    /// <param name="pages">Every page to consider, host-owned and packaged alike.</param>
    /// <param name="isAuthenticated">Whether the current caller is authenticated at all.</param>
    /// <param name="hasPermission">
    /// Answers whether the current caller holds a named permission. Invoked only by pages whose access rule
    /// names one.
    /// </param>
    /// <returns>Sections ordered by <see cref="INavItem.SectionOrder"/>, each holding its ordered pages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pages"/> or <paramref name="hasPermission"/> is null.</exception>
    public static IReadOnlyList<NavGroup> Build(
        IEnumerable<IPage> pages, bool isAuthenticated, Func<string, bool> hasPermission)
    {
        if (pages is null)
            throw new ArgumentNullException(nameof(pages));

        if (hasPermission is null)
            throw new ArgumentNullException(nameof(hasPermission));

        return pages
            .Where(p => !ReferenceEquals(p.NavItem, NavItem.Empty))
            .Where(p => p.Access.IsSatisfiedBy(isAuthenticated, hasPermission))
            .GroupBy(p => p.NavItem.SectionName, StringComparer.Ordinal)
            .Select(g => new NavGroup(
                g.Key,
                g.First().NavItem.SectionOrder,
                g.OrderBy(p => p.NavItem.Order).ToList()))
            .OrderBy(s => s.Order)
            .ToList();
    }
}
