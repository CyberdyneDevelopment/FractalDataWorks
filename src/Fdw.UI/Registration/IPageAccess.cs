using System;

namespace Fdw.UI.Registration;

/// <summary>
/// The rule deciding whether a caller may reach a page.
/// </summary>
// Why: an interface rather than the abstract class alone so IPage.Access is typed against an abstraction,
// the same way IPage.NavItem is typed against INavItem — and so this assembly stays free of any Blazor
// dependency, since the rule is answered from two plain facts a terminal renderer can supply as readily as
// a web one.
//
// Why two arguments rather than one permission check: they are two different axes. "Must the caller be
// authenticated at all" is axis 1; "given they are, what must they hold" is axis 2. Anonymous is a value on
// axis 1 and cannot be expressed as a permission, because an anonymous visitor holds no token and therefore
// no permission claim to check.
public interface IPageAccess
{
    /// <summary>
    /// Answers whether a caller in the supplied state may reach the page.
    /// </summary>
    /// <param name="isAuthenticated">Whether the caller is authenticated at all.</param>
    /// <param name="hasPermission">Answers whether the caller holds a named permission.</param>
    /// <returns>True when the caller may reach the page.</returns>
    bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission);
}
