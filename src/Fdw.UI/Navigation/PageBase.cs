using System;

namespace Fdw.UI.Navigation;

/// <summary>
/// Base class for a page declaration. Values arrive through the constructor.
/// </summary>
public abstract class PageBase : IPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageBase"/> class.
    /// </summary>
    /// <param name="name">The stable name for this page, unique within its owning page type.</param>
    /// <param name="component">The component type that renders the page.</param>
    /// <param name="navItem">The sidebar entry that opens it, or <see cref="NavItem.Empty"/> to keep it out of navigation.</param>
    /// <param name="access">The rule deciding who may reach it — a <see cref="PageAccess"/> form.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="component"/>, <paramref name="navItem"/>, or <paramref name="access"/> is null.</exception>
    protected PageBase(string name, Type component, INavItem navItem, IPageAccess access)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("A page requires a name.", nameof(name));

        Name = name;
        Component = component ?? throw new ArgumentNullException(nameof(component));
        NavItem = navItem ?? throw new ArgumentNullException(nameof(navItem));
        Access = access ?? throw new ArgumentNullException(nameof(access));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Type Component { get; }

    /// <inheritdoc />
    public INavItem NavItem { get; }

    /// <inheritdoc />
    public IPageAccess Access { get; }
}
