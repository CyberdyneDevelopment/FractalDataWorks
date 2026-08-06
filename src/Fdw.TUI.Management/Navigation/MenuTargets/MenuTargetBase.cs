using Fdw.Collections;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Abstract base class for menu targets.
/// Inherit from this class and apply [TypeOption] attribute to create menu targets.
/// </summary>
public abstract class MenuTargetBase : TypeOptionBase<int, MenuTargetBase>, IMenuTarget
{
    /// <summary>
    /// Creates a new menu target.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="name">Unique name used for lookup.</param>
    /// <param name="label">Display label shown in the menu.</param>
    /// <param name="group">Menu group for visual grouping.</param>
    /// <param name="order">Display order within the menu.</param>
    /// <param name="requiresConnection">Whether this target requires an active connection.</param>
    protected MenuTargetBase(
        int id,
        string name,
        string label,
        string group,
        int order,
        bool requiresConnection = false)
        : base(id, name)
    {
        Label = label;
        Group = group;
        Order = order;
        RequiresConnection = requiresConnection;
    }

    /// <inheritdoc />
    public string Label { get; }

    /// <inheritdoc />
    public string Group { get; }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public bool RequiresConnection { get; }

    /// <inheritdoc />
    public virtual bool IsAvailable => true;

    /// <inheritdoc />
    public abstract NavigationResult Navigate(IScreenFactory screenFactory);
}
