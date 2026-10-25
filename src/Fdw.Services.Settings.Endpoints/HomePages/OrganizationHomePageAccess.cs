using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Says whether the caller may use an organization's home page, and which scope holds it (FDW-2325).
/// </summary>
/// <remarks>
/// The caller must act in the organization, which also names its tenant (an organization that does not exist and one the caller does not
/// act in are refused alike), and must hold the right at that organization's scope.
/// </remarks>
internal static class OrganizationHomePageAccess
{
    internal const string Read = "home-page:read";

    internal const string Write = "home-page:write";

    internal static async Task<IGenericResult<HomePageScope>> Check(
        IOrganizationScope organizations, IScopeAuthority authority, Guid organizationId, string right, CancellationToken cancellationToken)
    {
        var tenant = await organizations.TenantOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<HomePageScope>();
        }

        var may = await authority.May(right, new ThingScope(tenant.Value, organizationId), cancellationToken).ConfigureAwait(false);
        return may.IsFailure
            ? may.ToNewResult<HomePageScope>()
            : GenericResult<HomePageScope>.Success(new HomePageScope(HomePageKinds.Organization, organizationId, tenant.Value));
    }
}
