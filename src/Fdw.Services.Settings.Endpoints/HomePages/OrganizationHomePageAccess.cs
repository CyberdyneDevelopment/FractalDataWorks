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
/// <see cref="IOrganizationScope"/> answers both questions at once: the organization must exist, and the caller must hold the right at its
/// scope; acting in the organization is not required (FDW-2840). An organization of another company and one that does not exist are
/// refused in the same way (FDW-2887).
/// </remarks>
internal static class OrganizationHomePageAccess
{
    internal const string Read = "home-page:read";

    internal const string Write = "home-page:write";

    internal static async Task<IGenericResult<HomePageScope>> Check(
        IOrganizationScope organizations, Guid organizationId, string right, CancellationToken cancellationToken)
    {
        var tenant = await organizations.TenantOf(right, organizationId, cancellationToken).ConfigureAwait(false);
        return tenant.IsFailure
            ? tenant.ToNewResult<HomePageScope>()
            : GenericResult<HomePageScope>.Success(new HomePageScope(HomePageKinds.Organization, organizationId, tenant.Value));
    }
}
