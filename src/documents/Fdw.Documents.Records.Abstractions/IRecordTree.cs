using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads and writes an organization's record tree (FDW-3395).</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IRecordTree
{
    /// <summary>Gets one node.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The node's public id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The node, or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id.</returns>
    Task<IGenericResult<IRecordNode>> Get(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists the children of a node, or the nodes at the top of the tree.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="parentId">The parent's public id, or null for the top of the tree.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The children, an empty list when the node has none; or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id as the parent.</returns>
    Task<IGenericResult<IReadOnlyList<IRecordNode>>> Children(Guid organizationId, Guid? parentId, CancellationToken cancellationToken = default);

    /// <summary>Saves a node, new or changed.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="node">The node.</param>
    /// <param name="expectedRevision">The organization's revision the caller read (<see cref="IOrganizationRevisions.Current"/>).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The organization's new revision, or a failure coded <c>DocumentRevisionConflict</c> when its records moved on since <paramref name="expectedRevision"/>, or <c>PublicIdInvalid</c> when the id is not a UUIDv7.</returns>
    Task<IGenericResult<long>> Save(Guid organizationId, IRecordNode node, long expectedRevision, CancellationToken cancellationToken = default);
}
