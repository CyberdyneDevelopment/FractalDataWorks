using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication;

/// <summary>
/// Default <see cref="IPrincipalResolver"/> — resolves the FDW claims principal by combining
/// tenant/org context with <c>IEffectivePermissionResolver</c> output and optional extra roles.
/// Shared across authentication implementations (OpenIddict, EasyAuth, …) via the core
/// Authentication package so the FDW claim shape is never duplicated.
/// </summary>
public sealed class DefaultPrincipalResolver : IPrincipalResolver
{
    // Why: this is a PERMISSION value, not a claim name. Kept as a local const so it does not
    // pollute ClaimDefinitions (which owns claim-name strings, not permission values).
    private const string ViewAllTenantsPermission = "tenants:view-all";
    // Why: UserTenantConfigurationProvider is Singleton — injecting Singleton into Scoped
    // (DefaultPrincipalResolver runs in scoped ProcessSignInClaimsHandler) is valid; the
    // provider has no per-request state. The former IServiceScopeFactory captive-dependency
    // dance is no longer needed.
    private readonly UserTenantConfigurationProvider _userTenantProvider;
    private readonly IOrganizationProvider _organizationProvider;
    private readonly IEffectivePermissionResolver _permissionResolver;
    private readonly UserRoleConfigurationProvider _userRoleProvider;
    private readonly RoleConfigurationProvider _roleProvider;
    private readonly ILogger<DefaultPrincipalResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPrincipalResolver"/> class.
    /// </summary>
    /// <param name="userTenantProvider">Resolves the user's tenant memberships and default tenant.</param>
    /// <param name="organizationProvider">Resolves and validates org context within a tenant.</param>
    /// <param name="permissionResolver">Resolves the effective (baked) permission set for the user.</param>
    /// <param name="userRoleProvider">Loads the user's role assignments.</param>
    /// <param name="roleProvider">Resolves role names from role ids.</param>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger{T}"/> when null.</param>
    public DefaultPrincipalResolver(
        UserTenantConfigurationProvider userTenantProvider,
        IOrganizationProvider organizationProvider,
        IEffectivePermissionResolver permissionResolver,
        UserRoleConfigurationProvider userRoleProvider,
        RoleConfigurationProvider roleProvider,
        ILogger<DefaultPrincipalResolver>? logger)
    {
        ArgumentNullException.ThrowIfNull(userTenantProvider);
        ArgumentNullException.ThrowIfNull(organizationProvider);
        ArgumentNullException.ThrowIfNull(permissionResolver);
        ArgumentNullException.ThrowIfNull(userRoleProvider);
        ArgumentNullException.ThrowIfNull(roleProvider);
        _userTenantProvider = userTenantProvider;
        _organizationProvider = organizationProvider;
        _permissionResolver = permissionResolver;
        _userRoleProvider = userRoleProvider;
        _roleProvider = roleProvider;
        _logger = logger ?? NullLogger<DefaultPrincipalResolver>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<ClaimsPrincipal>> Resolve(
        Guid userId,
        Guid? tenantId,
        Guid? orgId,
        IReadOnlyList<string> additionalRoles,
        CancellationToken cancellationToken = default)
        // Why: single-expression delegate avoids async state machine overhead.
        => Resolve(userId, tenantId, orgId, isCrossTenant: false, additionalRoles, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<ClaimsPrincipal>> Resolve(
        Guid userId,
        Guid? tenantId,
        Guid? orgId,
        bool isCrossTenant,
        IReadOnlyList<string> additionalRoles,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = userId.ToString();

        // Why: cross-tenant and single-tenant are mutually exclusive. A token cannot carry
        // both a specific tenant_id and the cross_tenant claim — that would be ambiguous.
        if (isCrossTenant && tenantId.HasValue)
            return GenericResult<ClaimsPrincipal>.Failure(
                PrincipalResolverLog.CrossTenantTenantConflict(_logger, userIdStr));

        if (isCrossTenant)
        {
            PrincipalResolverLog.ResolveBranchTrace(_logger, "cross-tenant", userIdStr, "(none)", "(none)", true);
            return await ResolveCrossTenant(userId, userIdStr, additionalRoles, cancellationToken).ConfigureAwait(false);
        }

        PrincipalResolverLog.ResolveBranchTrace(
            _logger,
            tenantId.HasValue ? "specific-tenant" : "default-tenant",
            userIdStr,
            tenantId?.ToString() ?? "(none)",
            orgId?.ToString() ?? "(none)",
            false);

        return await ResolveSingleTenant(userId, tenantId, orgId, userIdStr, additionalRoles, cancellationToken).ConfigureAwait(false);
    }

    // ── Single-tenant resolution ───────────────────────────────────────────────────

    private async Task<IGenericResult<ClaimsPrincipal>> ResolveSingleTenant(
        Guid userId,
        Guid? tenantId,
        Guid? orgId,
        string userIdStr,
        IReadOnlyList<string> additionalRoles,
        CancellationToken cancellationToken)
    {
        Guid resolvedTenantId;

        if (tenantId.HasValue)
        {
            // Why: When a specific tenant is requested, VALIDATE that the user belongs to it.
            // This is the security boundary — a forged or mismatched tenant_id is blocked here,
            // in addition to the RS256 signature block at the JWT layer.
            var tenantsResult = await _userTenantProvider.GetUserTenants(userId, cancellationToken).ConfigureAwait(false);
            if (!tenantsResult.IsSuccess)
                return GenericResult<ClaimsPrincipal>.Failure(
                    PrincipalResolverLog.TenantResolutionFailed(_logger, userIdStr, tenantsResult.CurrentMessage!));

            var membershipHolds = tenantsResult.Value!.Contains(tenantId.Value);
            PrincipalResolverLog.ResolveMembershipTrace(
                _logger, userIdStr, tenantId.Value.ToString(), tenantsResult.Value!.Count, membershipHolds ? "pass" : "deny");

            if (!membershipHolds)
                return GenericResult<ClaimsPrincipal>.Failure(
                    PrincipalResolverLog.TenantAccessDenied(_logger, userIdStr, tenantId.Value.ToString()));

            resolvedTenantId = tenantId.Value;
        }
        else
        {
            // Why: No tenant specified — resolve the user's default tenant (IsDefault=1).
            // We do NOT fall back to [0]; if there is no default row, the user has no tenants
            // and token issuance must fail loud.
            var defaultResult = await _userTenantProvider.GetDefaultTenant(userId, cancellationToken).ConfigureAwait(false);
            if (!defaultResult.IsSuccess)
                return GenericResult<ClaimsPrincipal>.Failure(
                    PrincipalResolverLog.TenantResolutionFailed(_logger, userIdStr, defaultResult.CurrentMessage!));

            if (defaultResult.Value is null)
                return GenericResult<ClaimsPrincipal>.Failure(
                    PrincipalResolverLog.NoTenantsForUser(_logger, userIdStr));

            resolvedTenantId = defaultResult.Value.Value;
            PrincipalResolverLog.ResolveDefaultTenantTrace(_logger, userIdStr, resolvedTenantId.ToString());
        }

        var orgValidation = await ResolveOrg(orgId, resolvedTenantId, cancellationToken).ConfigureAwait(false);
        if (!orgValidation.IsSuccess)
            return orgValidation.ToNewResult<ClaimsPrincipal>();
        var resolvedOrgId = orgValidation.Value;
        PrincipalResolverLog.ResolveOrgTrace(_logger, resolvedTenantId.ToString(), resolvedOrgId?.ToString() ?? "(none)");

        PrincipalResolverLog.PrincipalResolveStarted(
            _logger, userIdStr, resolvedTenantId.ToString(), resolvedOrgId?.ToString() ?? string.Empty);

        // Why: isGlobalTenant is always false here — token-issuance context is always tenant-scoped.
        // Global-tenant admins will have their global privileges via the role grant in authz.Role.
        var permResult = await _permissionResolver.Resolve(
            userIdStr, resolvedTenantId, resolvedOrgId, isGlobalTenant: false, cancellationToken).ConfigureAwait(false);

        if (!permResult.IsSuccess)
            return GenericResult<ClaimsPrincipal>.Failure(
                PrincipalResolverLog.PermissionResolutionFailed(_logger, userIdStr, permResult.CurrentMessage!));

        var allRoles = MergeRoles(additionalRoles, await LoadRoleNames(userId, cancellationToken).ConfigureAwait(false));

        PrincipalResolverLog.ResolvePermsTrace(
            _logger, userIdStr, resolvedTenantId.ToString(), permResult.Value!.Count, allRoles.Count);

        var claims = BuildClaims(userId, resolvedTenantId, resolvedOrgId, permResult.Value!, allRoles, isCrossTenant: false);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "FdwOpenIddict"));

        var roleClaimCount = claims.Count(c => string.Equals(c.Type, ClaimDefinitions.roles.Name, StringComparison.OrdinalIgnoreCase));
        var permClaimCount = claims.Count(c => string.Equals(c.Type, ClaimDefinitions.perm.Name, StringComparison.OrdinalIgnoreCase));
        PrincipalResolverLog.ResolveClaimsTrace(_logger, userIdStr, claims.Count, roleClaimCount, permClaimCount, false);

        PrincipalResolverLog.PrincipalResolved(
            _logger, userIdStr,
            resolvedTenantId.ToString(),
            resolvedOrgId?.ToString() ?? string.Empty,
            roleClaimCount,
            permClaimCount);

        return GenericResult<ClaimsPrincipal>.Success(principal);
    }

    // ── Org resolution helper ──────────────────────────────────────────────────────

    private async Task<IGenericResult<Guid?>> ResolveOrg(
        Guid? requestedOrgId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (requestedOrgId.HasValue)
        {
            // Why: When orgId is provided, validate it belongs to the resolved tenant.
            // A mismatched org would cause the RLS VisibilityGroup join to return nothing.
            var orgResult = await _organizationProvider.Get(requestedOrgId.Value, cancellationToken).ConfigureAwait(false);
            if (!orgResult.IsSuccess || orgResult.Value is null)
                return GenericResult<Guid?>.Failure(
                    PrincipalResolverLog.OrgResolutionFailed(_logger, tenantId.ToString(), orgResult.CurrentMessage ?? "(not found)"));

            if (orgResult.Value.TenantId != tenantId)
                return GenericResult<Guid?>.Failure(
                    PrincipalResolverLog.OrgTenantMismatch(_logger, requestedOrgId.Value.ToString(), tenantId.ToString()));

            return GenericResult<Guid?>.Success(requestedOrgId);
        }

        var defaultOrgResult = await _organizationProvider.GetDefault(tenantId, cancellationToken).ConfigureAwait(false);
        // Why: Missing default org is not fatal — some tenants have no orgs. Use null.
        return GenericResult<Guid?>.Success(defaultOrgResult.IsSuccess ? defaultOrgResult.Value?.Id : null);
    }

    // ── Role name loading ──────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<string>> LoadRoleNames(Guid userId, CancellationToken ct)
    {
        var assignmentsResult = await _userRoleProvider.GetByUser(userId.ToString(), ct).ConfigureAwait(false);
        if (!assignmentsResult.IsSuccess || assignmentsResult.Value is null)
            return Array.Empty<string>();

        // Why: GetRole(Guid id, ct) on DefaultConfigurationProvider incorrectly resolves
        // the WHERE column to ParentRoleId (authz.Role's self-referential FK) via
        // TryResolveFkColumnForGet, emitting WHERE ParentRoleId=@id and returning 0 rows.
        // Load all roles once and match by Id in memory — Get(ct) is cached per scope.
        var allRoles = await _roleProvider.GetAllRoles(ct).ConfigureAwait(false);

        var roleNames = new List<string>(assignmentsResult.Value.Count);
        foreach (var assignment in assignmentsResult.Value)
        {
            var role = allRoles.FirstOrDefault(r => r.Id == assignment.RoleId);
            if (role?.Name is { Length: > 0 } name)
                roleNames.Add(name);
        }
        return roleNames;
    }

    // Why: Extracted to keep ResolveSingleTenant and ResolveCrossTenant under the FDW007 complexity threshold.
    private static IReadOnlyList<string> MergeRoles(IReadOnlyList<string> additionalRoles, IReadOnlyList<string> loadedRoles)
    {
        if (additionalRoles.Count == 0)
            return loadedRoles;
        return additionalRoles.Concat(loadedRoles).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ── Cross-tenant resolution ────────────────────────────────────────────────────

    private async Task<IGenericResult<ClaimsPrincipal>> ResolveCrossTenant(
        Guid userId,
        string userIdStr,
        IReadOnlyList<string> additionalRoles,
        CancellationToken cancellationToken)
    {
        // Why: Cross-tenant requires tenants:view-all. We validate this by resolving permissions
        // in a non-tenant-scoped context — use the user's default tenant as the permission scope
        // (the permission is expected to be granted at the global-tenant or platform level).
        var defaultResult = await _userTenantProvider.GetDefaultTenant(userId, cancellationToken).ConfigureAwait(false);
        if (!defaultResult.IsSuccess)
            return GenericResult<ClaimsPrincipal>.Failure(
                PrincipalResolverLog.TenantResolutionFailed(_logger, userIdStr, defaultResult.CurrentMessage!));

        if (defaultResult.Value is null)
            return GenericResult<ClaimsPrincipal>.Failure(
                PrincipalResolverLog.NoTenantsForUser(_logger, userIdStr));

        PrincipalResolverLog.ResolveDefaultTenantTrace(_logger, userIdStr, defaultResult.Value.Value.ToString());

        var permResult = await _permissionResolver.Resolve(
            userIdStr, defaultResult.Value.Value, orgId: null, isGlobalTenant: false, cancellationToken).ConfigureAwait(false);

        if (!permResult.IsSuccess)
            return GenericResult<ClaimsPrincipal>.Failure(
                PrincipalResolverLog.PermissionResolutionFailed(_logger, userIdStr, permResult.CurrentMessage!));

        var holdsViewAll = permResult.Value!.Any(p => string.Equals(p, ViewAllTenantsPermission, StringComparison.OrdinalIgnoreCase));
        PrincipalResolverLog.ResolveCrossTenantGateTrace(_logger, userIdStr, holdsViewAll, permResult.Value!.Count);

        // Why: Cross-tenant is only allowed when the user holds tenants:view-all.
        // Fail loud — do NOT silently strip the cross-tenant request and issue a single-tenant token.
        if (!holdsViewAll)
            return GenericResult<ClaimsPrincipal>.Failure(
                PrincipalResolverLog.CrossTenantAccessDenied(_logger, userIdStr));

        var allRoles = MergeRoles(additionalRoles, await LoadRoleNames(userId, cancellationToken).ConfigureAwait(false));

        var claims = BuildClaims(userId, tenantId: null, orgId: null, permResult.Value!, allRoles, isCrossTenant: true);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "FdwOpenIddict"));

        var permClaimCount = claims.Count(c => string.Equals(c.Type, ClaimDefinitions.perm.Name, StringComparison.OrdinalIgnoreCase));
        PrincipalResolverLog.ResolveClaimsTrace(
            _logger, userIdStr, claims.Count,
            claims.Count(c => string.Equals(c.Type, ClaimDefinitions.roles.Name, StringComparison.OrdinalIgnoreCase)),
            permClaimCount, true);

        PrincipalResolverLog.CrossTenantPrincipalResolved(_logger, userIdStr, permClaimCount);

        return GenericResult<ClaimsPrincipal>.Success(principal);
    }

    private static List<Claim> BuildClaims(
        Guid userId,
        Guid? tenantId,
        Guid? orgId,
        IReadOnlyCollection<string> permissions,
        IReadOnlyList<string> additionalRoles,
        bool isCrossTenant)
    {
        var claims = new List<Claim>(capacity: 5 + permissions.Count + additionalRoles.Count)
        {
            new(ClaimDefinitions.sub.Name, userId.ToString()),
        };

        // Why: Mutually exclusive — cross-tenant tokens have no single active tenantId.
        // Single-tenant tokens always have a tenantId (resolvedTenantId is non-null there).
        if (isCrossTenant)
            claims.Add(new Claim(ClaimDefinitions.crossTenant.Name, "true"));
        else if (tenantId.HasValue)
            claims.Add(new Claim(ClaimDefinitions.tenantId.Name, tenantId.Value.ToString()));

        if (!isCrossTenant && orgId.HasValue)
            claims.Add(new Claim(ClaimDefinitions.orgId.Name, orgId.Value.ToString()));

        foreach (var role in additionalRoles)
            claims.Add(new Claim(ClaimDefinitions.roles.Name, role));

        foreach (var perm in permissions)
            claims.Add(new Claim(ClaimDefinitions.perm.Name, perm));

        return claims;
    }
}
