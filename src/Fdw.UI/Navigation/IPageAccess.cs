using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The rule deciding whether a caller may reach a page.
/// </summary>
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
