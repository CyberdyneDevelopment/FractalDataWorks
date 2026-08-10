using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The rule for a page anyone may reach. Declared as <see cref="PageAccess.Anonymous"/>.
/// </summary>
// Why a type whose rule is the constant true, rather than the absence of a rule: absence is what the
// previous null permission meant, and it already meant "any authenticated user". A page open to the public
// has to say so, and saying so is not the same as saying nothing.
internal sealed class AnonymousPageAccess : PageAccess
{
    /// <inheritdoc />
    // Why neither argument is read: no caller state can fail this rule. That is the whole content of
    // "anonymous" — not that the checks pass, but that there are none.
    public override bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission) => true;
}
