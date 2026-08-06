using System;
using System.Security.Claims;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Multitenancy.Services;

/// <summary>
/// Default implementation of <see cref="IRequestTenantInfo"/> that extracts tenant from JWT claims.
/// </summary>
/// <remarks>
/// Always-on infrastructure — registered by every <see cref="MultitenancyTypeBase{TFactory}"/> option
/// (SingleTenant and Sql alike), since request-scoped tenant/admin info is meaningful even when no
/// real tenant store is configured (an admin-role check on <c>CurrentUsername</c> still applies).
/// </remarks>
public sealed class RequestTenantInfo : IRequestTenantInfo
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>Initializes a new instance of the <see cref="RequestTenantInfo"/> class.</summary>
    public RequestTenantInfo(IHttpContextAccessor httpContextAccessor, ISystemRoleConfiguration systemRoleConfiguration)
    {
        _httpContextAccessor = httpContextAccessor;
        _systemRoleConfiguration = systemRoleConfiguration;
    }

    /// <inheritdoc/>
    public Guid? CurrentTenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return null;

            var tenantClaim = user.FindFirst(ClaimDefinitions.tenantId.Name)
                ?? user.FindFirst("TenantId")
                ?? user.FindFirst("tid");

            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
                return tenantId;

            return null;
        }
    }

    /// <inheritdoc/>
    public bool IsSystemAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return false;

            // Why: delegate to ISystemRoleConfiguration so the admin role name is resolved from
            // deployment configuration rather than hardcoded. IsInRole uses the principal's
            // RoleClaimType which the FDW token validator sets to ClaimDefinitions.roles.Name.
            return _systemRoleConfiguration.IsInRole(user, _systemRoleConfiguration.AdminRoleName);
        }
    }

    /// <inheritdoc/>
    public string? CurrentUsername
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return null;

            return user.Identity?.Name
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;
        }
    }
}
