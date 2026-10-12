using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Null-object implementation of <see cref="ITenantProvider"/>, registered by the SingleTenant
/// multitenancy option. All operations return failure so callers that guard on <c>IsSuccess</c>
/// skip tenant resolution without crashing.
/// </summary>
public sealed class NullTenantProvider : ITenantProvider
{
    /// <inheritdoc />
    public Task<IGenericResult<ITenant>> GetTenant(Guid tenantId, CancellationToken ct = default)
        => Task.FromResult<IGenericResult<ITenant>>(
            GenericResult<ITenant>.Failure(TenantTypesLog.TenantLookupUnavailable(NullLogger.Instance)));

    /// <inheritdoc />
    public Task<IGenericResult<ITenant>> GetTenantBySlug(string slug, CancellationToken ct = default)
        => Task.FromResult<IGenericResult<ITenant>>(
            GenericResult<ITenant>.Failure(TenantTypesLog.TenantLookupUnavailable(NullLogger.Instance)));

    /// <inheritdoc />
    public Task<IGenericResult<IEnumerable<ITenant>>> GetActiveTenants(CancellationToken ct = default)
        => Task.FromResult<IGenericResult<IEnumerable<ITenant>>>(
            GenericResult<IEnumerable<ITenant>>.Failure(TenantTypesLog.TenantLookupUnavailable(NullLogger.Instance)));

    /// <inheritdoc />
    public Task<IGenericResult<IEnumerable<ITenant>>> GetAllTenants(CancellationToken ct = default)
        => Task.FromResult<IGenericResult<IEnumerable<ITenant>>>(
            GenericResult<IEnumerable<ITenant>>.Failure(TenantTypesLog.TenantLookupUnavailable(NullLogger.Instance)));

    /// <inheritdoc />
    public Task<IGenericResult<bool>> ValidateTenantAccess(
        Guid tenantId,
        string userId,
        CancellationToken ct = default)
        => Task.FromResult<IGenericResult<bool>>(
            GenericResult<bool>.Failure(TenantTypesLog.TenantLookupUnavailable(NullLogger.Instance)));

    /// <inheritdoc />
    public Task<IGenericResult<ITenant>> ResolveTenant(
        ITenantResolutionContext context,
        CancellationToken ct = default)
        => Task.FromResult<IGenericResult<ITenant>>(
            GenericResult<ITenant>.Failure(TenantTypesLog.TenantLookupUnavailable(NullLogger.Instance)));
}
