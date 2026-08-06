using Fdw.Collections;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Base class for navigation actions.
/// </summary>
public abstract class NavigationActionBase : TypeOptionBase<int, NavigationActionBase>, INavigationAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="NavigationActionBase"/>.
    /// </summary>
    protected NavigationActionBase(int id, string name) : base(id, name) { }
}
