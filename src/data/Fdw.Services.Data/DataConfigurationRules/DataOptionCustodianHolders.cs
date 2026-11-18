using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>
/// Who may be a data option rule's custodian: the holders of <c>datastores:write</c> (FDW-6346, Release-Supervisor 2026-11-17: the picker and the
/// check use the same holders). The caller's tenant answers when the caller has one; a caller with no tenant is answered by the platform's holders,
/// the other scope <see cref="IPermissionHolders"/> declares. A holders list that cannot be read is the failure, never an empty list.
/// </summary>
internal static class DataOptionCustodianHolders
{
    private const string Permission = "datastores:write";

    /// <summary>Gets the ids of the people who hold <c>datastores:write</c> for the caller's tenant, or on the platform when the caller has no tenant.</summary>
    /// <param name="holders">The directory of who holds a permission.</param>
    /// <param name="tenant">The caller's tenant.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The holders' ids; or why they could not be read.</returns>
    public static Task<IGenericResult<IReadOnlyList<Guid>>> Get(IPermissionHolders holders, ITenantContext tenant, CancellationToken cancellationToken = default)
        => tenant.TenantId is { } tenantId
            ? holders.Get(Permission, tenantId, cancellationToken)
            : holders.GetOnPlatform(Permission, cancellationToken);
}
