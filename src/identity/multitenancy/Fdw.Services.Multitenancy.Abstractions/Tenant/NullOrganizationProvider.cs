using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Null-object implementation of <see cref="IOrganizationProvider"/>.
/// Registered when multitenancy is disabled. All queries return failure so callers
/// that guard on <c>IsSuccess</c> will skip the org tier without crashing.
/// </summary>
public sealed class NullOrganizationProvider : IOrganizationProvider
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly NullOrganizationProvider Instance = new();

    /// <inheritdoc />
    public Task<IGenericResult<OrganizationConfiguration>> Get(Guid orgId, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<OrganizationConfiguration>>(
            GenericResult<OrganizationConfiguration>.Failure(
                TenantTypesLog.OrgLookupUnavailable(NullLogger.Instance)));

    /// <inheritdoc />
    public Task<IGenericResult<OrganizationConfiguration>> GetDefault(Guid tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<OrganizationConfiguration>>(
            GenericResult<OrganizationConfiguration>.Failure(
                TenantTypesLog.OrgLookupUnavailable(NullLogger.Instance)));
}
