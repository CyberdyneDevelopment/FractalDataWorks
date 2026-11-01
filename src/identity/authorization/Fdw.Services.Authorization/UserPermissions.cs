using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Default implementation of <see cref="IUserPermissions"/>.
/// Returns the permission set for the roles the user is ACTUALLY ASSIGNED IN THE TENANT they are
/// acting in — not the entire catalog, and not the roles they hold in any other tenant — plus the
/// org tier when an org is named.
/// Invoked by <c>BakePermissionsStepType</c> at token issuance and by
/// <c>ApiKeyAuthenticationHandler</c> to bake the permission set onto the caller.
/// </summary>
public sealed class UserPermissions : IUserPermissions
{
    private readonly IUserPermissionGrants _grants;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IOrgAccessProvider _orgAccessProvider;
    private readonly ILogger<UserPermissions> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="UserPermissions"/>.
    /// </summary>
    public UserPermissions(
        IUserPermissionGrants grants,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<UserPermissions>? logger,
        IOrgAccessProvider? orgAccessProvider = null)
    {
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<UserPermissions>.Instance;
        _orgAccessProvider = orgAccessProvider ?? NullOrgAccessProvider.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EffectiveAuthorization>> Get(
        string userId,
        Guid? tenantId,
        Guid? orgId,
        CancellationToken cancellationToken = default)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        // A Guid stored as VARCHAR: compared as text the match depends on how each side spelled
        // it, so both are compared as the Guids they are (FDW-532) -- in UserPermissionGrants, the one
        // place that says which rows are a user's. An unparseable subject matches nothing, which is
        // what it should do.
        _ = Guid.TryParse(userId, out var subjectId);
        var grantsResult = await _grants.Get(subjectId, cancellationToken).ConfigureAwait(false);
        if (!grantsResult.IsSuccess || grantsResult.Value is null)
            return GenericResult<EffectiveAuthorization>.Failure(
                AuthorizationLog.UserRoleAssignmentLoadFailed(_logger, userId));

        var grants = grantsResult.Value;
        var assignedRoles = grants.Select(g => g.RoleId).Distinct().Count();
        AuthorizationLog.UserRoleAssignmentsLoaded(_logger, assignedRoles, userId);

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var globalCount = 0;
        var tenantCount = 0;
        DateTimeOffset? validUntil = null;

        if (assignedRoles == 0)
        {
            AuthorizationLog.UserHasNoRoleAssignments(_logger, userId);
        }
        else
        {
            // A role is held PER TENANT (FDW-825): only the assignments made in the tenant the caller
            // is acting in count. Being an Admin of AFC grants nothing while acting in NFC, and --
            // the case this used to get wrong -- nothing while acting in the global tenant either:
            // acting in global carries the roles held IN global, not the union of every tenant's.
            // A caller acting in no tenant holds no roles.
            //
            // An assignment also stops counting once it expires (ExpiresAt in the past). This is a
            // framework guarantee, not a front-end one: FDW is consumed by callers other than our own
            // console, so an expiry the schema offers must be honored here rather than left for a
            // caller to enforce or not.
            var counted = grants
                .Where(g => tenantId.HasValue && g.AssignmentTenantId == tenantId.Value
                            && (g.ExpiresAt is null || g.ExpiresAt > DateTimeOffset.UtcNow))
                .ToList();

            // What counted now stops counting at its expiry, so the result is true only until the
            // earliest one (FDW-862). Min over nullable values skips the assignments that never expire.
            validUntil = counted.Min(g => g.ExpiresAt);

            (globalCount, tenantCount) = ApplyRoleTiers(counted, tenantId, permissions, roleNames);
        }

        var orgResult = await ApplyOrgTier(
            userId, tenantId, orgId, permissions, cancellationToken).ConfigureAwait(false);

        if (orgResult.IsFailure)
            return orgResult.ToNewResult<EffectiveAuthorization>();

        AuthorizationLog.ThreeTierPermissionsResolved(_logger, globalCount, tenantCount, orgResult.Value, permissions.Count, userId);

        return GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(permissions, roleNames, validUntil));
    }

    private static (int GlobalCount, int TenantCount) ApplyRoleTiers(
        IReadOnlyList<UserPermissionGrant> counted,
        Guid? currentTenantId,
        HashSet<string> permissions,
        HashSet<string> roleNames)
    {
        var globalPermCount = 0;
        var tenantPermCount = 0;

        foreach (var role in counted.GroupBy(g => g.RoleId))
        {
            var first = role.First();

            // A platform role (not tenant-scoped) can be assigned in any tenant. A tenant-scoped role
            // is one tenant's own definition and contributes only in that tenant. What a role grants
            // is what its owning tenant wrote: the view counts no grant any other tenant owns.
            var roleIsGlobal = !first.IsTenantScoped;
            if (!roleIsGlobal && !(currentTenantId.HasValue && first.RoleTenantId == currentTenantId.Value))
                continue;

            roleNames.Add(first.RoleName);

            foreach (var grant in role)
            {
                if (grant.PermissionName is { } permission && permissions.Add(permission))
                {
                    if (roleIsGlobal) globalPermCount++;
                    else tenantPermCount++;
                }
            }
        }

        return (globalPermCount, tenantPermCount);
    }

    private async Task<IGenericResult<int>> ApplyOrgTier(
        string userId,
        Guid? currentTenantId,
        Guid? orgId,
        HashSet<string> permissions,
        CancellationToken cancellationToken)
    {
        if (!orgId.HasValue || !currentTenantId.HasValue)
        {
            AuthorizationLog.OrgTierSkippedNoOrgContext(_logger, userId);
            return GenericResult<int>.Success(0);
        }

        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            AuthorizationLog.OrgTierSkippedNonGuidUserId(_logger, userId);
            return GenericResult<int>.Success(0);
        }

        var orgGrantsResult = await _orgAccessProvider.Get(
            userIdGuid, currentTenantId.Value, orgId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (orgGrantsResult.IsFailure)
            return orgGrantsResult.ToNewResult<int>();
        if (orgGrantsResult.Value is null)
            return GenericResult<int>.Failure(AuthorizationLog.OrgAccessQueryFailed(_logger, userIdGuid, orgId.Value));

        var orgPermCount = 0;
        foreach (var grant in orgGrantsResult.Value)
        {
            orgPermCount += ApplyOrgGrant(grant, permissions);
        }
        return GenericResult<int>.Success(orgPermCount);
    }

    /// <summary>Adds the permission an org-tier grant confers.</summary>
    /// <remarks>
    /// A grant's RoleName is not read. It is descriptive on this table -- it records why the
    /// access was granted, and the RLS predicate that consumes these rows tests their existence
    /// rather than either name. The values seeded into it (PlatformAdmin, TenantMember) are not
    /// in the role catalogue and never were, so the lookup that used to sit here missed for
    /// every row and contributed nothing, silently, while PermissionName carried the tier.
    /// Restoring it means deciding those role names are real and seeding them; until then a
    /// lookup that cannot succeed is worse than no lookup, because it reads as one that works.
    /// </remarks>
    private static int ApplyOrgGrant(
        TenantOrgAccessConfiguration grant,
        HashSet<string> permissions)
        => !string.IsNullOrEmpty(grant.PermissionName) && permissions.Add(grant.PermissionName)
            ? 1
            : 0;
}
