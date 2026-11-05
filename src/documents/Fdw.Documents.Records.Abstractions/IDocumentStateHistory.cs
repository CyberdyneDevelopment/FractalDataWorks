using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads a document's state history and moves the document into a new state (FDW-3395).</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IDocumentStateHistory
{
    /// <summary>Lists a document's state changes, oldest first.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document's public id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The changes, or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id.</returns>
    Task<IGenericResult<IReadOnlyList<IStateChange>>> List(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Moves a document into a state, recording the change.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="change">The change.</param>
    /// <param name="expectedRevision">The organization's revision the caller read (<see cref="IOrganizationRevisions.Current"/>).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The organization's new revision, or a failure coded <c>DocumentRevisionConflict</c> when its records moved on since <paramref name="expectedRevision"/>, or <c>PublicIdInvalid</c> when the id is not a UUIDv7.</returns>
    Task<IGenericResult<long>> Enter(Guid organizationId, IStateChange change, long expectedRevision, CancellationToken cancellationToken = default);
}
