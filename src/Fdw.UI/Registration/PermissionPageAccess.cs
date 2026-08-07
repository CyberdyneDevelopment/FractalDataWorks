using System;

namespace Fdw.UI.Registration;

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
        // Why rejected rather than treated as "no permission required": an empty permission is an author
        // who meant to name one, and silently widening the page to every authenticated caller is the
        // failure this whole type exists to make impossible. Same reasoning as PageBase rejecting an empty
        // name.
        if (string.IsNullOrEmpty(permission))
            throw new ArgumentException("A permission-gated page requires a permission name.", nameof(permission));

        _permission = permission;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hasPermission"/> is null.</exception>
    // Why isAuthenticated is checked before the permission: the two axes are separate, and a caller who is
    // not authenticated holds no claims at all — asking the predicate about them would be asking a question
    // whose answer cannot be meaningful.
    public override bool IsSatisfiedBy(bool isAuthenticated, Func<string, bool> hasPermission)
    {
        // Why guarded here and not only by the caller: this is the one form that actually invokes the
        // predicate, and a missing predicate is a caller who cannot answer the question — not a caller who
        // answers "no". Failing is the only honest response; defaulting either way decides a security
        // question by omission.
        if (hasPermission is null)
            throw new ArgumentNullException(nameof(hasPermission));

        return isAuthenticated && hasPermission(_permission);
    }
}
