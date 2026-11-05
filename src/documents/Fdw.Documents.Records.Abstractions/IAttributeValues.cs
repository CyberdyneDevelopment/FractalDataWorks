using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads and sets the attribute values of an organization's documents and record nodes (FDW-3395).</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IAttributeValues
{
    /// <summary>Lists the values of one document or node.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="ownerId">The public id of the document or node.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The values, or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id as the owner.</returns>
    Task<IGenericResult<IReadOnlyList<IAttributeValue>>> List(Guid organizationId, Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>Sets one value, replacing the owner's value of the same attribute code.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="value">The value.</param>
    /// <param name="expectedRevision">The organization's revision the caller read (<see cref="IOrganizationRevisions.Current"/>).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The organization's new revision, or a failure coded <c>DocumentRevisionConflict</c> when its records moved on since <paramref name="expectedRevision"/>, or <c>PublicIdInvalid</c> when the id is not a UUIDv7.</returns>
    Task<IGenericResult<long>> Set(Guid organizationId, IAttributeValue value, long expectedRevision, CancellationToken cancellationToken = default);
}
