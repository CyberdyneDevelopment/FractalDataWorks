using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Configuration;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// The one lookup of the assignment an organization has in force for a purpose (FDW-1581, FDW-3396, FDW-3673): the live version of the row,
/// not deleted and not superseded. Document files (<see cref="DocumentStores"/>) and document records (<see cref="DocumentRecordStores"/>)
/// both find their organization's connection through it.
/// </summary>
internal static class AssignmentsInForce
{
    /// <summary>Finds an organization's assignment in force for a purpose.</summary>
    /// <param name="assignments">The assignments.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="purpose">The purpose.</param>
    /// <param name="logger">The logger of the caller.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The assignment, Success with null when the organization has none for the purpose, or the read's failure.</returns>
    public static async Task<IGenericResult<IConnectionAssignmentImplementationConfiguration?>> For(
        IConnectionAssignmentConfigurationProvider assignments, Guid organizationId, string purpose, ILogger logger, CancellationToken cancellationToken)
    {
        var found = await assignments.Find<ConnectionAssignmentImplementationConfiguration>(
            assignment => assignment.OrganizationId == organizationId && assignment.IsCurrent && !assignment.IsDeleted && assignment.SupersededAt is null
                && string.Equals(assignment.Purpose, purpose, StringComparison.OrdinalIgnoreCase), cancellationToken).ConfigureAwait(false);
        return found.IsSuccess && found.Value is { } rows
            ? GenericResult<IConnectionAssignmentImplementationConfiguration?>.Success(rows.Count > 0 ? rows[0] : null)
            : GenericResult<IConnectionAssignmentImplementationConfiguration?>.Failure(
                DocumentStoreLog.ReadFailed(logger, $"The connection assigned to organization '{organizationId}'", found.CurrentMessage));
    }
}
