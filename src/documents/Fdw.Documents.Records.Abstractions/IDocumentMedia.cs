using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads and adds the files of an organization's documents (FDW-3395). The bytes are in the organization's document store; this holds the record of each.</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IDocumentMedia
{
    /// <summary>Lists a document's files.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document's public id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The files, or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id.</returns>
    Task<IGenericResult<IReadOnlyList<IStoredMedia>>> List(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Adds the record of a file written to the organization's document store.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="media">The file, with the assignment its store was resolved through.</param>
    /// <param name="expectedRevision">The organization's revision the caller read (<see cref="IOrganizationRevisions.Current"/>).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The organization's new revision, or a failure coded <c>DocumentRevisionConflict</c> when its records moved on since <paramref name="expectedRevision"/>, or <c>PublicIdInvalid</c> when the id is not a UUIDv7.</returns>
    Task<IGenericResult<long>> Add(Guid organizationId, IStoredMedia media, long expectedRevision, CancellationToken cancellationToken = default);
}
