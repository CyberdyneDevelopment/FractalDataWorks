using Fdw.Collections;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Base class for permission action type options.
/// </summary>
public abstract class PermissionActionBase : TypeOptionBase<int, PermissionActionBase>, IPermissionAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionActionBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this action.</param>
    /// <param name="name">The action name (e.g., "Read", "Write").</param>
    /// <param name="icon">The MudBlazor icon name.</param>
    /// <param name="color">The MudBlazor color.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="isWriteAction">Whether this implies write access.</param>
    /// <param name="isDestructive">Whether this is potentially destructive.</param>
    protected PermissionActionBase(
        int id,
        string name,
        string icon,
        string color,
        string description,
        bool isWriteAction,
        bool isDestructive)
        : base(id, name)
    {
        Icon = icon;
        Color = color;
        Description = description;
        IsWriteAction = isWriteAction;
        IsDestructive = isDestructive;
    }

    /// <inheritdoc />
    public string Icon { get; }

    /// <inheritdoc />
    public string Color { get; }

    /// <inheritdoc />
    public new string Description { get; }

    /// <inheritdoc />
    public bool IsWriteAction { get; }

    /// <inheritdoc />
    public bool IsDestructive { get; }
}
