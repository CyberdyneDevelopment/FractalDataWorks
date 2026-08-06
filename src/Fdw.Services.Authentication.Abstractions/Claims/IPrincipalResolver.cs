using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Resolves the FDW <see cref="ClaimsPrincipal"/> for a given user identity by:
/// <list type="bullet">
///   <item>Resolving the user's active tenant (and optionally org).</item>
///   <item>Calling <c>IEffectivePermissionResolver</c> to get the baked permission set.</item>
///   <item>Building a <see cref="ClaimsPrincipal"/> with sub, tenant_id, org_id, role, and perm claims.</item>
/// </list>
/// Both credential paths (password/agent_key) and the external-identity path funnel through
/// this resolver so ALL issued tokens share the same FDW claim shape.
/// FAIL-LOUD: missing tenant, missing permissions → non-success result (no token issued without full context).
/// </summary>
public interface IPrincipalResolver
{
    /// <summary>
    /// Resolves the FDW <see cref="ClaimsPrincipal"/> for <paramref name="userId"/>.
    /// </summary>
    /// <param name="userId">The FDW user GUID (drives the sub claim and RLS).</param>
    /// <param name="tenantId">
    /// Explicit tenant override. When <see langword="null"/>, the resolver picks the
    /// user's first active tenant. Fail-loud if no tenants exist.
    /// </param>
    /// <param name="orgId">
    /// Explicit org override. When <see langword="null"/>, the resolver uses the default org
    /// for the resolved tenant (or no org if none is configured).
    /// </param>
    /// <param name="additionalRoles">
    /// Extra roles added on top of the database-resolved role set (e.g., <c>["agent"]</c>
    /// for agent-key grants). May be empty.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Success with the fully-populated <see cref="ClaimsPrincipal"/>, or a non-success
    /// result if the tenant is missing or permission resolution fails.
    /// </returns>
    Task<IGenericResult<ClaimsPrincipal>> Resolve(
        Guid userId,
        Guid? tenantId,
        Guid? orgId,
        IReadOnlyList<string> additionalRoles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the FDW <see cref="ClaimsPrincipal"/> with explicit cross-tenant flag.
    /// </summary>
    /// <param name="userId">The FDW user GUID.</param>
    /// <param name="tenantId">
    /// Explicit tenant override. Must be <see langword="null"/> when <paramref name="isCrossTenant"/> is true.
    /// </param>
    /// <param name="orgId">Explicit org override. Must be <see langword="null"/> when <paramref name="isCrossTenant"/> is true.</param>
    /// <param name="isCrossTenant">
    /// When <see langword="true"/>, issues a cross-tenant token (requires <c>tenants:view-all</c> permission).
    /// Mutually exclusive with a non-null <paramref name="tenantId"/>.
    /// </param>
    /// <param name="additionalRoles">Extra roles to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<ClaimsPrincipal>> Resolve(
        Guid userId,
        Guid? tenantId,
        Guid? orgId,
        bool isCrossTenant,
        IReadOnlyList<string> additionalRoles,
        CancellationToken cancellationToken = default);
}
