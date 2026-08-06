using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Resolves the effective permission set for a user by applying the 3-tier union:
/// global-tenant ∪ current-tenant ∪ current-org.
/// Called at TOKEN-ISSUE time so the resolved set can be baked into the JWT as <c>perm</c> claims.
/// Also called per-request by the authorization service for the non-baked path.
/// </summary>
public interface IEffectivePermissionResolver
{
    /// <summary>
    /// Resolves the full effective permission set for the given user context.
    /// </summary>
    /// <param name="userId">The user identifier (numeric string, matches <c>usr.Users.Id</c>).</param>
    /// <param name="tenantId">The tenant the user is operating under. <see langword="null"/> collapses to global-only tier.</param>
    /// <param name="orgId">The org the user is operating under. <see langword="null"/> skips the org tier.</param>
    /// <param name="isGlobalTenant">
    ///     <see langword="true"/> when the current tenant is the global/home tenant.
    ///     Global-tenant callers see all role grants regardless of <c>IsTenantScoped</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     On success, the union of all three permission tiers.
    ///     On failure (any provider query failed), returns a failure result — callers must
    ///     treat failure as "deny access" (fail-closed).
    /// </returns>
    Task<IGenericResult<IReadOnlyCollection<string>>> Resolve(
        string userId,
        Guid? tenantId,
        Guid? orgId,
        bool isGlobalTenant,
        CancellationToken cancellationToken = default);
}
