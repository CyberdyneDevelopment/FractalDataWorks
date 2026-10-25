using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Reads what the roles a person is assigned grant (FDW-2688). The read asks the database for that person's rows only, with the role and
/// permission rows already joined, so its cost does not grow with the number of people, roles or permissions.
/// </summary>
public interface IUserPermissionGrants
{
    /// <summary>Reads one row for each permission each role the person is assigned grants, in every tenant, expired or not.</summary>
    /// <param name="userId">The person's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The grants; none when the person is assigned no role; or the failure when the read failed.</returns>
    Task<IGenericResult<IReadOnlyList<UserPermissionGrant>>> Get(Guid userId, CancellationToken cancellationToken = default);
}
