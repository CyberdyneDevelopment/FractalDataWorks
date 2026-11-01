using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The access rules a page can declare: <see cref="Anonymous"/>, <see cref="Authenticated"/>, or
/// <see cref="RequiringPermission"/>.
/// </summary>
public abstract class PageAccess : IPageAccess
{
    private protected PageAccess()
    {
    }

    /// <summary>
    /// Reachable by anyone, including a caller who is not authenticated.
    /// </summary>
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
    public static IPageAccess RequiringPermission(string permission) => new PermissionPageAccess(permission);

    /// <inheritdoc />
    public abstract bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission);
}
