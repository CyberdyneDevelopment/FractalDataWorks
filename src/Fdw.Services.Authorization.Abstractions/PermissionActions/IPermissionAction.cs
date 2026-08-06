using Fdw.Collections;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Interface for permission action types with UI rendering properties.
/// </summary>
/// <remarks>
/// <para>
/// Permission actions define the standard set of operations that can be performed
/// on resources. Each action includes visual properties for consistent rendering.
/// </para>
/// <para>
/// Standard actions: Read, Write, Delete, Execute, Browse, Create, Update, Admin, Approve, Manage.
/// </para>
/// </remarks>
public interface IPermissionAction : ITypeOption<int, PermissionActionBase>
{
    /// <summary>
    /// Gets the MudBlazor icon name for this action.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets the MudBlazor color for this action.
    /// </summary>
    /// <example>Primary, Success, Info, Warning, Error</example>
    string Color { get; }

    /// <summary>
    /// Gets a human-readable description of this action.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets whether this action implies write access.
    /// </summary>
    /// <remarks>
    /// Write actions include: Write, Create, Update, Delete, Admin, Manage, Approve.
    /// Read actions include: Read, Browse, Execute.
    /// </remarks>
    bool IsWriteAction { get; }

    /// <summary>
    /// Gets whether this action is potentially destructive.
    /// </summary>
    bool IsDestructive { get; }
}
