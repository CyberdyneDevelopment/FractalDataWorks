using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads and writes an organization's document records (FDW-3395). The application implements it over its own storage.</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IDocumentRecords
{
    /// <summary>Gets one document.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The document's public id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The document, or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id.</returns>
    Task<IGenericResult<IDocumentRecord>> Get(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists the organization's documents of one type.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentType">The document type.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The documents, an empty list when the organization has none of the type.</returns>
    Task<IGenericResult<IReadOnlyList<IDocumentRecord>>> List(Guid organizationId, string documentType, CancellationToken cancellationToken = default);

    /// <summary>Saves a document, new or changed.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="document">The document; its id is the application's, checked with <see cref="PublicIds.Check"/>.</param>
    /// <param name="expectedRevision">The organization's revision the caller read (<see cref="IOrganizationRevisions.Current"/>).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The organization's new revision, or a failure coded <c>DocumentRevisionConflict</c> when its records moved on since <paramref name="expectedRevision"/>, or <c>PublicIdInvalid</c> when the id is not a UUIDv7.</returns>
    Task<IGenericResult<long>> Save(Guid organizationId, IDocumentRecord document, long expectedRevision, CancellationToken cancellationToken = default);
}
