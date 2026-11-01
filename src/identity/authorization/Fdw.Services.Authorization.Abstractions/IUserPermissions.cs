using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Resolves the effective permission set for a user acting in one tenant: the roles they hold IN
/// THAT TENANT, plus the org tier when an org is named. Roles are held per tenant — a token carries
/// only the permissions of the tenant it was issued for (FDW-825).
/// Called at TOKEN-ISSUE time so the resolved set can be baked into the JWT as <c>perm</c> claims.
/// Also called per-request by the authorization service for the non-baked path.
/// </summary>
public interface IUserPermissions
{
    /// <summary>
    /// Resolves the full effective permission set for the given user context.
    /// </summary>
    /// <param name="userId">The user identifier (numeric string, matches <c>usr.Users.Id</c>).</param>
    /// <param name="tenantId">
    /// The tenant the user is acting in. Only role assignments made in this tenant count — acting in
    /// the global tenant carries the roles held in the global tenant, never the union of every
    /// tenant's. <see langword="null"/> means no tenant, and holds no roles.
    /// </param>
    /// <param name="orgId">The org the user is operating under. <see langword="null"/> skips the org tier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     On success, the permissions of the roles held in <paramref name="tenantId"/> (and the org
    ///     tier) plus those roles' names.
    ///     On failure (any provider query failed), returns a failure result — callers must
    ///     treat failure as "deny access" (fail-closed).
    /// </returns>
    Task<IGenericResult<EffectiveAuthorization>> Get(
        string userId,
        Guid? tenantId,
        Guid? orgId,
        CancellationToken cancellationToken = default);
}
