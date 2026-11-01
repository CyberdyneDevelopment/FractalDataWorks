using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Users.Abstractions;

/// <summary>
/// Reads the tenants one person is a member of (FDW-2676). The read asks the database for that person's memberships only, so its cost does not grow
/// with the number of people.
/// </summary>
public interface IUserTenantsHeld
{
    /// <summary>Reads the memberships that name a person.</summary>
    /// <param name="userId">The person's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenants the person is a member of, each with whether it is their default; none when the person is a member of none; or the failure when the read failed.</returns>
    Task<IGenericResult<IReadOnlyList<HeldTenant>>> Get(Guid userId, CancellationToken cancellationToken = default);
}
