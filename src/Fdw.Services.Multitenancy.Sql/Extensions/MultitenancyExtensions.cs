using Fdw.Services.Multitenancy.Sql.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Fdw.Services.Multitenancy.Sql.Extensions;

/// <summary>
/// Extension methods for the multi-tenancy middleware pipeline.
/// </summary>
/// <remarks>
/// Service registration (tenant/org contexts, ITenantProvider, IOrganizationProvider, ...) is no
/// longer done here — it is a ServiceTypeCollection domain (see <see cref="Fdw.Services.Multitenancy.MultitenancyTypes"/>,
/// <see cref="Fdw.Services.Multitenancy.SingleTenantMultitenancyType"/>,
/// <c>SqlMultitenancyType</c> (now ReferenceMultitenancy.Sql)). The entry point resolves the host's configured
/// <c>Multitenancy</c> option and calls its <c>Register</c> phase directly.
/// </remarks>
public static class MultitenancyExtensions
{
    /// <summary>
    /// Adds the tenant resolution middleware to the pipeline.
    /// Includes <c>OrgResolutionMiddleware</c> immediately after tenant resolution.
    /// </summary>
    public static IApplicationBuilder UseMultitenancy(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantResolutionMiddleware>();
        // Why: OrgResolutionMiddleware must run immediately after TenantResolutionMiddleware so
        // IOrgContext is populated before authorization and endpoint handlers execute.
        app.UseMiddleware<OrgResolutionMiddleware>();
        return app;
    }
}
