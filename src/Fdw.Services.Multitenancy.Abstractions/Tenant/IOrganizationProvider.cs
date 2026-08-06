using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Provides access to organization records backed by <c>tenant.Organizations</c>.
/// </summary>
public interface IOrganizationProvider
{
    /// <summary>
    /// Gets an organization by its logical identifier.
    /// </summary>
    Task<IGenericResult<OrganizationConfiguration>> Get(Guid orgId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default organization for a tenant (the one with <c>IsDefault=1</c>).
    /// </summary>
    Task<IGenericResult<OrganizationConfiguration>> GetDefault(Guid tenantId, CancellationToken cancellationToken = default);
}
