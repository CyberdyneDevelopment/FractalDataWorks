using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Null-object implementation of <see cref="ITenantProvider"/>: every operation fails with a coded "not available", so a caller that guards on <c>IsSuccess</c>
/// skips tenant resolution without crashing.
/// </summary>
/// <remarks>
/// Nothing that ships registers it (FDW-6528). The SingleTenant multitenancy option used to, and a host that ran it could not judge a run started for a person, because
/// the judge asks the tenant provider whether the person may act in the tenant and whether it is the global one; the option now registers the configured provider, which
/// answers both from the configuration database. It stays for a consumer that builds its own host and wants a provider that answers nothing.
/// </remarks>
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
    public Task<IGenericResult<IEnumerable<ITenant>>> GetDeletedTenants(CancellationToken ct = default)
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
    public Task<IGenericResult<bool>> ValidateOrganizationAccess(
        Guid tenantId,
        Guid organizationId,
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
