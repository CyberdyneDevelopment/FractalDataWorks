using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Provides tenant resolution and management.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    Task<IGenericResult<ITenant>> GetTenant(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Gets a tenant by slug.
    /// </summary>
    Task<IGenericResult<ITenant>> GetTenantBySlug(string slug, CancellationToken ct = default);

    /// <summary>
    /// Gets all active tenants.
    /// </summary>
    Task<IGenericResult<IEnumerable<ITenant>>> GetActiveTenants(CancellationToken ct = default);

    /// <summary>
    /// Gets all tenants (including inactive).
    /// </summary>
    Task<IGenericResult<IEnumerable<ITenant>>> GetAllTenants(CancellationToken ct = default);

    /// <summary>
    /// Validates that a user has access to a tenant.
    /// </summary>
    Task<IGenericResult<bool>> ValidateTenantAccess(
        Guid tenantId,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates that a user may act in a department (organization) of a tenant (FDW-1802): the department exists, belongs to that
    /// tenant, is active, and the user holds access in it.
    /// </summary>
    /// <param name="tenantId">The tenant the token is for.</param>
    /// <param name="organizationId">The department asked for.</param>
    /// <param name="userId">The user, as the Guid text of their id.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>True when the user may act there, false when they may not, or a failure when the question could not be answered.</returns>
    Task<IGenericResult<bool>> ValidateOrganizationAccess(
        Guid tenantId,
        Guid organizationId,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves tenant from a request (hostname, header, route, etc.).
    /// </summary>
    Task<IGenericResult<ITenant>> ResolveTenant(
        ITenantResolutionContext context,
        CancellationToken ct = default);
}
