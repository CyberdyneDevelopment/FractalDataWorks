using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The access rules a page can declare: <see cref="Anonymous"/>, <see cref="Authenticated"/>, or
/// <see cref="RequiringPermission"/>.
/// </summary>
// Why a closed family of types rather than a flag beside a permission string: the combinations a flag
// admits include "anonymous, but holding permission x:y", which is not a state the auth model has. Each
// form is a separate type carrying exactly the data its own rule needs, so the meaningless combination is
// unrepresentable rather than accepted and then rejected by a validator.
//
// Why the rule lives on the type: every consumer calls IsSatisfiedBy and the form it lands on decides. No
// caller switches on which form it holds — a three-branch chain over the kinds is precisely what FDW017—019
// steer away from, and it would have to be repeated by every renderer.
//
// Why not a TypeCollection, the usual reach in this codebase: [TypeOption]s are name-registered singletons,
// and RequiringPermission carries per-page data. A singleton cannot hold a different permission per page.
public abstract class PageAccess : IPageAccess
{
    // Why private protected: the three forms below are the whole family. Leaving the constructor open would
    // let a fourth form appear outside this assembly with a rule no renderer's declarations anticipated,
    // which is the openness this design exists to remove.
    private protected PageAccess()
    {
    }

    /// <summary>
    /// Reachable by anyone, including a caller who is not authenticated.
    /// </summary>
    // Why a singleton, exposed as the interface: the form carries no data, so every page declaring it wants
    // the same instance — the same shape as NavItem.Empty, and compared the same way if a consumer ever
    // needs to.
    public static IPageAccess Anonymous { get; } = new AnonymousPageAccess();

    /// <summary>
    /// Reachable by any authenticated caller, whatever permissions they hold.
    /// </summary>
    public static IPageAccess Authenticated { get; } = new AuthenticatedPageAccess();

    /// <summary>
    /// Reachable by an authenticated caller holding the named permission.
    /// </summary>
    /// <param name="permission">The permission the caller must hold, in <c>{resource}:{action}</c> form.</param>
    /// <returns>The access rule for that permission.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="permission"/> is null or empty.</exception>
    // Why not a singleton: this form carries the permission, so each page's is its own instance.
    public static IPageAccess RequiringPermission(string permission) => new PermissionPageAccess(permission);

    /// <inheritdoc />
    public abstract bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission);
}
