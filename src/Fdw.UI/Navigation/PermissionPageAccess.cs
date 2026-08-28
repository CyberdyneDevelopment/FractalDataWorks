using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// The rule for a page reachable by an authenticated caller holding a named permission. Declared as
/// <see cref="PageAccess.RequiringPermission"/>.
/// </summary>
internal sealed class PermissionPageAccess : PageAccess
{
    private readonly string _permission;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionPageAccess"/> class.
    /// </summary>
    /// <param name="permission">The permission the caller must hold.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="permission"/> is null or empty.</exception>
    internal PermissionPageAccess(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            throw new ArgumentException("A permission-gated page requires a permission name.", nameof(permission));

        _permission = permission;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hasPermission"/> is null.</exception>
    public override bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission)
    {
        if (hasPermission is null)
            throw new ArgumentNullException(nameof(hasPermission));

        return isAuthenticated && hasPermission(_permission);
    }
}
