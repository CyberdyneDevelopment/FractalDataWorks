using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Where a workspace is in its lifecycle.</summary>
public interface IWorkspaceStatus : ITypeOption<int, WorkspaceStatusBase>
{
    /// <summary>Gets whether a workspace in this status is closed: it can no longer be the scope writes are decided at.</summary>
    /// <remarks>
    /// A closed workspace gives back what it owns of a kind that is released on archive (FDW-6967), such as a path or a container of a shared
    /// data store. The status states it, so nothing compares status names.
    /// </remarks>
    bool IsClosed { get; }
}
