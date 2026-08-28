using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The rule for a page anyone may reach. Declared as <see cref="PageAccess.Anonymous"/>.
/// </summary>
internal sealed class AnonymousPageAccess : PageAccess
{
    /// <inheritdoc />
    public override bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission) => true;
}
