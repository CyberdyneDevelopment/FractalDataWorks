using System;

namespace Fdw.UI.Registration;

/// <summary>
/// Base class for a page declaration. Values arrive through the constructor.
/// </summary>
// Why: constructor arguments, not settable or overridable properties — the same way every other FDW
// option carries its values. No parameter has a default, so a page author states "no nav entry" and
// "no permission" deliberately by passing null rather than inheriting either by omission.
public abstract class PageBase : IPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageBase"/> class.
    /// </summary>
    /// <param name="name">The stable name for this page, unique within its owning page type.</param>
    /// <param name="component">The component type that renders the page.</param>
    /// <param name="navItem">The sidebar entry that opens it, or <see cref="NavItem.Empty"/> to keep it out of navigation.</param>
    /// <param name="requiredPermission">The permission required to reach it, or null for any authenticated user.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="component"/> or <paramref name="navItem"/> is null.</exception>
    protected PageBase(string name, Type component, INavItem navItem, string? requiredPermission)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("A page requires a name.", nameof(name));

        Name = name;
        // Why: the renderer resolves the page's address from this type, so a null component yields a nav
        // entry that cannot navigate anywhere. Reject it here rather than at first click.
        Component = component ?? throw new ArgumentNullException(nameof(component));
        // Why: absence is NavItem.Empty, never null — a null here means the author skipped the decision
        // rather than declaring "not in navigation", so it is rejected instead of silently substituted.
        NavItem = navItem ?? throw new ArgumentNullException(nameof(navItem));
        RequiredPermission = requiredPermission;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Type Component { get; }

    /// <inheritdoc />
    public INavItem NavItem { get; }

    /// <inheritdoc />
    public string? RequiredPermission { get; }
}
