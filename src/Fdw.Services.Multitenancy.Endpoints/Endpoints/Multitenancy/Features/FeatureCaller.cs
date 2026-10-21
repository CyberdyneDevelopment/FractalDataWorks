using System;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Who may act for a tenant's features (FDW-1580): a platform administrator for any tenant, anyone else for the tenant they act in.
/// </summary>
internal static class FeatureCaller
{
    internal static bool MayActFor(ClaimsPrincipal caller, Guid tenantId, ISystemRoleConfiguration systemRoles)
        => (Guid.TryParse(caller.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var activeTenant) && activeTenant == tenantId)
            || systemRoles.IsPlatformAdmin(caller);
}
