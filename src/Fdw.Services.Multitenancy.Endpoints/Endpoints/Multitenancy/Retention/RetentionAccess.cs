using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Who may act on an organization's retention (FDW-1575): a platform administrator for any organization, anyone else for the
/// organizations of the tenant they act in. The right to do the thing itself is the endpoint's policy.
/// </summary>
internal static class RetentionAccess
{
    // The tenant of the organization when the caller may act for it; otherwise the response that refuses the caller.
    internal static async Task<(Guid TenantId, IResult? Refusal)> TenantOf(
        IOrganizationProvider organizations, ISystemRoleConfiguration systemRoles, HttpContext httpContext, Guid organizationId, ILogger logger,
        CancellationToken cancellationToken)
    {
        var organization = await organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization.IsFailure || organization.Value is null)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(organization, httpContext);
            return (Guid.Empty, TypedResults.Json(problem, statusCode: statusCode));
        }

        if (!FeatureCaller.MayActFor(httpContext.User, organization.Value.TenantId, systemRoles))
        {
            RetentionEndpointLog.OtherTenant(logger, organization.Value.TenantId);
            return (Guid.Empty, TypedResults.StatusCode(StatusCodes.Status403Forbidden));
        }

        return (organization.Value.TenantId, null);
    }
}
