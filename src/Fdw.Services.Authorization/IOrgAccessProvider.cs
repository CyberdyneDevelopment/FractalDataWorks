using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>
/// Provides org-tier access grants from <c>tenant.TenantOrgAccess</c>.
/// Used by <see cref="DefaultAuthorizationService"/> to build the org-scoped tier of the
/// effective permission set.
/// </summary>
public interface IOrgAccessProvider
{
    /// <summary>
    /// Returns all access grants for the given user in the given tenant-org combination.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>> Get(
        Guid userId,
        Guid tenantId,
        Guid orgId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every access grant the given user holds, across all tenants and orgs.
    /// </summary>
    /// <remarks>
    /// This is the membership question — "where does this user belong" — as opposed to the
    /// three-argument overload's "what does this user hold here", which requires the caller to
    /// already know the tenant and org. A caller that needs to enumerate a user's tenants has no
    /// way to ask the narrower overload, because the tenant is the answer, not the input.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>> Get(
        Guid userId,
        CancellationToken cancellationToken = default);
}
