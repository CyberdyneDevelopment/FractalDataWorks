using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Reads the roles one person holds (FDW-2668). The read asks the database for that person's assignments only, so its cost does not grow with the
/// number of people.
/// </summary>
public interface IUserRolesHeld
{
    /// <summary>Reads the assignments that name a person, in every tenant, expired or not.</summary>
    /// <param name="userId">The person's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles the person is assigned; none when the person is assigned none; or the failure when the read failed.</returns>
    Task<IGenericResult<IReadOnlyList<HeldRole>>> Get(Guid userId, CancellationToken cancellationToken = default);
}
