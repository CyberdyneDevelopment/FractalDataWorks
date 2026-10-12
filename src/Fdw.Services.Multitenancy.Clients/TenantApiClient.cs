namespace Fdw.Services.Multitenancy.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for multitenancy operations.
/// </summary>
public sealed class TenantApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantApiClient"/> class.
    /// </summary>
    public TenantApiClient(HttpClient httpClient, ILogger<TenantApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets a list of all tenants.
    /// </summary>
    /// <returns>A result containing the list of tenant summaries.</returns>
    public Task<IGenericResult<IReadOnlyList<TenantSummaryPayload>>> GetTenants(bool includeInactive = false, CancellationToken ct = default)
        => GetList<TenantSummaryPayload>($"tenants?includeInactive={includeInactive}", ct);

    /// <summary>
    /// Gets a specific tenant by its unique identifier.
    /// </summary>
    /// <returns>A result containing the tenant detail.</returns>
    public Task<IGenericResult<TenantDetailPayload>> GetTenant(Guid tenantId, CancellationToken ct = default)
        => Get<TenantDetailPayload>($"tenants/{tenantId}", ct);

    /// <summary>
    /// Gets the current authenticated user's tenant.
    /// </summary>
    /// <returns>A result containing the current tenant detail.</returns>
    public Task<IGenericResult<TenantDetailPayload>> GetCurrentTenant(CancellationToken ct = default)
        => Get<TenantDetailPayload>("tenants/current", ct);

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <returns>A result containing the created tenant detail.</returns>
    public Task<IGenericResult<TenantDetailPayload>> CreateTenant(CreateTenantRequest request, CancellationToken ct = default)
        => Post<CreateTenantRequest, TenantDetailPayload>("tenants", request, ct);

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <returns>A result containing the updated tenant detail.</returns>
    public Task<IGenericResult<TenantDetailPayload>> UpdateTenant(Guid tenantId, UpdateTenantRequest request, CancellationToken ct = default)
        => Patch<UpdateTenantRequest, TenantDetailPayload>($"tenants/{tenantId}", request, ct);

    /// <summary>
    /// Switches the current user's active tenant.
    /// </summary>
    /// <returns>A result containing the switch tenant response.</returns>
    public Task<IGenericResult<SwitchTenantResponse>> SwitchTenant(SwitchTenantRequest request, CancellationToken ct = default)
        => Post<SwitchTenantRequest, SwitchTenantResponse>("tenants/switch", request, ct);

    /// <summary>
    /// Sets the specified tenant as the caller's default tenant.
    /// The caller must already be a member of the tenant.
    /// </summary>
    /// <returns>A result containing the confirmed default tenant identifier.</returns>
    public Task<IGenericResult<SetDefaultTenantResponse>> SetDefaultTenant(Guid tenantId, CancellationToken ct = default)
        => Post<object, SetDefaultTenantResponse>($"tenants/{tenantId}/default", new object(), ct);
}
