using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Says whether the caller may use a tenant's home page (FDW-2471). The scope authority holds that the caller acts in the tenant and holds the right
/// there; a tenant is its own scope, so the home page of tenant T is held at T.
/// </summary>
internal static class TenantHomePageAccess
{
    internal static async Task<IGenericResult<HomePageScope>> Check(IScopeAuthority authority, Guid tenantId, string right, CancellationToken cancellationToken)
    {
        var may = await authority.May(right, new ThingScope(tenantId, null), cancellationToken).ConfigureAwait(false);
        return may.IsFailure
            ? may.ToNewResult<HomePageScope>()
            : GenericResult<HomePageScope>.Success(new HomePageScope(HomePageKinds.Tenant, tenantId, tenantId));
    }
}
