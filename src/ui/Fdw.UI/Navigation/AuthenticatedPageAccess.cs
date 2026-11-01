using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The rule for a page any authenticated caller may reach. Declared as
/// <see cref="PageAccess.Authenticated"/>.
/// </summary>
internal sealed class AuthenticatedPageAccess : PageAccess
{
    /// <inheritdoc />
    public override bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission) => isAuthenticated;
}
