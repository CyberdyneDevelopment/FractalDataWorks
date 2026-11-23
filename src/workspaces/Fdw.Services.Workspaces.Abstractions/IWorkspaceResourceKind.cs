using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>
/// A kind of resource that can be attached to a workspace.
/// </summary>
public interface IWorkspaceResourceKind : ITypeOption<int, WorkspaceResourceKindBase>
{
    /// <summary>
    /// Gets whether a workspace can own a resource of this kind, as opposed to only using it.
    /// </summary>
    /// <remarks>
    /// A workspace owns the data sets it sketched, so archiving the project takes them with it. It
    /// does not own the shared connection it reads through — that belongs to the platform and
    /// serves every other project too. Ownership is therefore a property of the kind, not a free
    /// choice at attach time, and attaching with the wrong relationship is a structured failure.
    /// </remarks>
    bool CanBeOwned { get; }

    /// <summary>
    /// Gets whether archiving the workspace that owns a resource of this kind releases it, so the workspace owns it no more.
    /// </summary>
    /// <remarks>
    /// A data set a workspace sketched is the workspace's and stays with it when the workspace is archived. A path or a
    /// container of a shared data store is the store's, lent to a workspace to own: a closed workspace cannot be the scope
    /// its writes are decided at, so archiving gives it back to the store (FDW-6967). The kind states which, so the
    /// archive asks the kind and never a list of kind names.
    /// </remarks>
    bool ReleasedOnArchive { get; }
}
