using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>Reads the revision of an organization's document records (FDW-3395), which every write names to detect a concurrent change.</summary>
/// <remarks>A missing row is a failed result with a code, never null. FDW names no table: the application's implementation holds the storage.</remarks>
public interface IOrganizationRevisions
{
    /// <summary>Gets the organization's current revision.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The revision, or a failure coded <c>DocumentRecordNotFound</c> when the organization's records were never started (<see cref="Start"/>).</returns>
    Task<IGenericResult<IOrganizationRevision>> Current(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an organization's document records at its first revision. Done once, when the organization begins to keep documents; there
    /// is no revision to assume before it.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The first revision, or a failure coded <c>DocumentRevisionConflict</c> when the organization's records were already started.</returns>
    Task<IGenericResult<IOrganizationRevision>> Start(Guid organizationId, CancellationToken cancellationToken = default);
}
