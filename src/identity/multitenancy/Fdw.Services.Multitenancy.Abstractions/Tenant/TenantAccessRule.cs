using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The one answer to "may this person act in this tenant" (FDW-825, FDW-6528): the tenant must exist and be active, and the person must be a member of it or a member
/// of the global tenant, which may act as any tenant. Every tenant provider that answers <see cref="ITenantProvider.ValidateTenantAccess"/> from stored rows asks it, so
/// two providers cannot give two answers over the same rows.
/// </summary>
/// <remarks>
/// It judges rows it is given and reads nothing. It never answers "member" for a person it was not told is one: the tenant a run acts in comes from the request, and a
/// removed membership is a revoked one, so the person's memberships are read when the run is judged and handed here.
/// </remarks>
public static class TenantAccessRule
{
    /// <summary>Says whether a person may act in a tenant.</summary>
    /// <param name="tenantId">The tenant the person is to act in.</param>
    /// <param name="tenants">The tenants that exist and are not deleted.</param>
    /// <param name="memberOf">The ids of the tenants the person has a current membership in.</param>
    /// <returns><see langword="true"/> when the tenant exists and is active and the person is a member of it or of the global tenant.</returns>
    public static bool Allows(Guid tenantId, IReadOnlyCollection<TenantFact> tenants, IReadOnlyCollection<Guid> memberOf)
        => tenants.FirstOrDefault(tenant => tenant.Id == tenantId) is { IsActive: true }
            && (memberOf.Contains(tenantId) || tenants.Any(tenant => tenant.IsGlobal && memberOf.Contains(tenant.Id)));
}
