using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for <see cref="DefaultPrincipalResolver"/> — tenant/org resolution, permission
/// baking, and the cross-tenant gate. Lives in the core Authentication package alongside the
/// resolver so every authentication implementation (OpenIddict, EasyAuth, …) that reuses the
/// resolver shares the same log surface without duplication.
/// EventId range: 7330–7335, 7400–7403, 7408–7414 (relocated from OpenIddictProviderLog), plus 7000.
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class PrincipalResolverLog
{
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning,
        Message = "Principal resolve rejected for userId={userId}: IsCrossTenant and TenantId are mutually exclusive — a cross-tenant token has no single active tenant.")]
    internal static partial IGenericMessage CrossTenantTenantConflict(ILogger logger, string userId);

    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "Resolving FDW principal for userId={userId} tenantId={tenantId} orgId={orgId}.")]
    internal static partial IGenericMessage PrincipalResolveStarted(ILogger logger, string userId, string tenantId, string orgId);

    [MessageLogging(EventId = 11013, Level = LogLevel.Information,
        Message = "FDW principal resolved: userId={userId} tenantId={tenantId} orgId={orgId} roles={roleCount} perms={permCount}.")]
    internal static partial IGenericMessage PrincipalResolved(ILogger logger, string userId, string tenantId, string orgId, int roleCount, int permCount);

    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "Permission resolution failed for userId={userId}: {message}")]
    internal static partial IGenericMessage PermissionResolutionFailed(ILogger logger, string userId, string message);

    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "User tenant resolution failed for userId={userId}: {message}")]
    internal static partial IGenericMessage TenantResolutionFailed(ILogger logger, string userId, string message);

    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "No tenants found for userId={userId}. Cannot issue token without tenant context.")]
    internal static partial IGenericMessage NoTenantsForUser(ILogger logger, string userId);

    [MessageLogging(EventId = 91010, Level = LogLevel.Error,
        Message = "Default org resolution failed for tenantId={tenantId}: {message}")]
    internal static partial IGenericMessage OrgResolutionFailed(ILogger logger, string tenantId, string message);

    [MessageLogging(EventId = 51000, Level = LogLevel.Warning,
        Message = "Tenant access denied: userId={userId} requested tenantId={tenantId} but is not a member of that tenant.")]
    internal static partial IGenericMessage TenantAccessDenied(ILogger logger, string userId, string tenantId);

    [MessageLogging(EventId = 51001, Level = LogLevel.Warning,
        Message = "Cross-tenant access denied: userId={userId} does not hold 'tenants:view-all' permission.")]
    internal static partial IGenericMessage CrossTenantAccessDenied(ILogger logger, string userId);

    [MessageLogging(EventId = 41000, Level = LogLevel.Error,
        Message = "Org validation failed: orgId={orgId} does not belong to tenantId={tenantId}.")]
    internal static partial IGenericMessage OrgTenantMismatch(ILogger logger, string orgId, string tenantId);

    [MessageLogging(EventId = 11014, Level = LogLevel.Trace,
        Message = "Cross-tenant principal resolved for userId={userId} with {permCount} permissions across all accessible tenants.")]
    internal static partial IGenericMessage CrossTenantPrincipalResolved(ILogger logger, string userId, int permCount);

    /// <summary>Traces which top-level resolve branch was taken.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace,
        Message = "Principal resolve: branch={branch} userId={userId} requestedTenant={requestedTenant} requestedOrg={requestedOrg} isCrossTenant={isCrossTenant}.")]
    internal static partial IGenericMessage ResolveBranchTrace(ILogger logger, string branch, string userId, string requestedTenant, string requestedOrg, bool isCrossTenant);

    /// <summary>Traces a specific-tenant membership check outcome.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace,
        Message = "Principal resolve membership check: userId={userId} requestedTenant={tenantId} tenantsFound={tenantsFound} membership={result}.")]
    internal static partial IGenericMessage ResolveMembershipTrace(ILogger logger, string userId, string tenantId, int tenantsFound, string result);

    /// <summary>Traces default-tenant selection outcome.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace,
        Message = "Principal resolve default tenant: userId={userId} selectedDefault={tenantId}.")]
    internal static partial IGenericMessage ResolveDefaultTenantTrace(ILogger logger, string userId, string tenantId);

    /// <summary>Traces org resolution outcome.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace,
        Message = "Principal resolve org: tenantId={tenantId} resolvedOrg={orgId}.")]
    internal static partial IGenericMessage ResolveOrgTrace(ILogger logger, string tenantId, string orgId);

    /// <summary>Traces the permission resolution outcome (perms/roles baked).</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace,
        Message = "Principal resolve perms: userId={userId} tenantId={tenantId} permsBaked={permCount} extraRoles={roleCount}.")]
    internal static partial IGenericMessage ResolvePermsTrace(ILogger logger, string userId, string tenantId, int permCount, int roleCount);

    /// <summary>Traces the cross-tenant view-all gate outcome.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace,
        Message = "Principal resolve cross-tenant gate: userId={userId} holdsViewAll={holdsViewAll} permsBaked={permCount}.")]
    internal static partial IGenericMessage ResolveCrossTenantGateTrace(ILogger logger, string userId, bool holdsViewAll, int permCount);

    /// <summary>Traces the final claim set assembled before return.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace,
        Message = "Principal resolve claims: userId={userId} totalClaims={claimCount} roles={roleCount} perms={permCount} isCrossTenant={isCrossTenant}.")]
    internal static partial IGenericMessage ResolveClaimsTrace(ILogger logger, string userId, int claimCount, int roleCount, int permCount, bool isCrossTenant);
}
