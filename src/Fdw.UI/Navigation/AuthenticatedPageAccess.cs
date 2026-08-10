using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The rule for a page any authenticated caller may reach. Declared as
/// <see cref="PageAccess.Authenticated"/>.
/// </summary>
internal sealed class AuthenticatedPageAccess : PageAccess
{
    /// <inheritdoc />
    // Why hasPermission is not consulted: this rule is entirely on the "is the caller authenticated at all"
    // axis. Which permissions they hold is the other axis, and this form declares nothing about it.
    public override bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission) => isAuthenticated;
}
