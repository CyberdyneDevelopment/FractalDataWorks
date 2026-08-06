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
}
