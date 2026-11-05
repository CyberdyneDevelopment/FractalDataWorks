using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads and writes the documents an organization's record nodes are expected to have (FDW-3395).</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IRecordRequirements
{
    /// <summary>Lists a node's requirements.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="recordId">The node's public id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The requirements, or a failure coded <c>DocumentRecordNotFound</c> when the organization has none of that id as the node.</returns>
    Task<IGenericResult<IReadOnlyList<IRecordRequirement>>> List(Guid organizationId, Guid recordId, CancellationToken cancellationToken = default);

    /// <summary>Saves a requirement, new or satisfied.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="requirement">The requirement.</param>
    /// <param name="expectedRevision">The organization's revision the caller read (<see cref="IOrganizationRevisions.Current"/>).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The organization's new revision, or a failure coded <c>DocumentRevisionConflict</c> when its records moved on since <paramref name="expectedRevision"/>, or <c>PublicIdInvalid</c> when the id is not a UUIDv7.</returns>
    Task<IGenericResult<long>> Save(Guid organizationId, IRecordRequirement requirement, long expectedRevision, CancellationToken cancellationToken = default);
}
