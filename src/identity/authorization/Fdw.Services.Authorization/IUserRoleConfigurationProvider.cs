using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the configured UserRole members.</summary>
public interface IUserRoleConfigurationProvider
    : IDomainConfigurationProvider<IUserRoleImplementationConfiguration>
{
    /// <summary>Removes, in <paramref name="unit"/>, every assignment of a role to a person (FDW-5222).</summary>
    /// <param name="roleId">The durable Id of the role whose assignments go.</param>
    /// <param name="unit">The unit the removal joins; it commits or rolls back with the rest of the unit.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>How many assignments were removed; otherwise the failure of the read or of the first removal that failed.</returns>
    /// <remarks>
    /// For the delete of the role itself: an assignment left behind names a role that no longer exists, and the holder's own profile
    /// then fails to read. The assignments are read before anything in the unit is written, by the role delete that calls this.
    /// </remarks>
    Task<IGenericResult<int>> RemoveAssignmentsOf(Guid roleId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);
}
