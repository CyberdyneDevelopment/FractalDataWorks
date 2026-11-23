using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Base for Where a workspace is in its lifecycle.</summary>
/// <remarks>
/// No id is passed — <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name. Nothing persists that id; the database stores the option's NAME, so the
/// name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class WorkspaceStatusBase : TypeOptionBase<WorkspaceStatusBase>, IWorkspaceStatus
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceStatusBase"/> class.</summary>
    /// <param name="name">The option name, which is the value persisted.</param>
    /// <param name="isClosed">Whether a workspace in this status is closed and gives back what it owns of a kind released on archive.</param>
    protected WorkspaceStatusBase(string name, bool isClosed) : base(name)
    {
        IsClosed = isClosed;
    }

    /// <inheritdoc />
    public bool IsClosed { get; }
}
