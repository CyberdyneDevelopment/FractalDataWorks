using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Reads and writes which workspace owns a resource: the <c>Owns</c> rows of <c>workspace.WorkspaceResource</c> (FDW-6967).
/// </summary>
/// <remarks>
/// Ownership is the same row for every kind a workspace may own, so one service in the workspace package reads and writes it, and a domain
/// that lends part of what it owns to a workspace (a path or a container of a shared data store) asks it instead of reading the workspace's
/// tables. It decides nothing about who may do what: the caller asks <see cref="IWorkspaceAccessPolicy"/> first.
/// </remarks>
public interface IWorkspaceResourceOwners
{
    /// <summary>Reads every workspace that holds a current <c>Owns</c> row for one resource.</summary>
    /// <param name="kind">The resource's kind, a <c>WorkspaceResourceKinds</c> name.</param>
    /// <param name="resourceId">The resource's Id.</param>
    /// <param name="tenantId">The tenant the resource belongs to. The rows are read as that tenant, so the answer is the same whoever asks.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The owners, none when nobody owns it; a failure when the rows cannot be read.</returns>
    Task<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>> Owners(string kind, Guid resourceId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Reads every current <c>Owns</c> row of one kind in a tenant: what workspaces own of that kind, in one read.</summary>
    /// <param name="kind">The kind, a <c>WorkspaceResourceKinds</c> name.</param>
    /// <param name="tenantId">The tenant whose rows are read, read as that tenant so the answer is the same whoever asks.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The owners, none when nothing of the kind is owned; a failure when the rows cannot be read.</returns>
    Task<IGenericResult<IReadOnlyList<WorkspaceResourceOwner>>> Owned(string kind, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Records <paramref name="workspace"/> as the owner of one resource.</summary>
    /// <param name="workspace">The workspace, as read.</param>
    /// <param name="kind">The resource's kind; it must be a kind a workspace may own.</param>
    /// <param name="resourceId">The resource's Id.</param>
    /// <param name="label">The resource's name, stored on the row as its label.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The <c>Owns</c> row written; a failure when the kind is unknown or cannot be owned, the caller is not known, or the write fails.</returns>
    Task<IGenericResult<WorkspaceResourceConfiguration>> Own(
        IWorkspaceImplementationConfiguration workspace, string kind, Guid resourceId, string label, CancellationToken cancellationToken = default);

    /// <summary>Retires one resource row by its Id; for an <c>Owns</c> row, the resource is owned by that workspace no more.</summary>
    /// <param name="owner">The owner, as <see cref="Owners"/> read it.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a failure when the row cannot be retired.</returns>
    Task<IGenericResult> Release(WorkspaceResourceOwner owner, CancellationToken cancellationToken = default);

    /// <summary>Releases every resource <paramref name="workspace"/> owns whose kind is released when its owner is archived.</summary>
    /// <param name="workspace">The workspace being archived, with its resources loaded.</param>
    /// <param name="cancellationToken">Cancels the writes.</param>
    /// <returns>The resource rows released, none when it owns no such resource; a failure at the first row that cannot be retired.</returns>
    Task<IGenericResult<IReadOnlyList<WorkspaceResourceConfiguration>>> ReleaseOnArchive(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default);
}
