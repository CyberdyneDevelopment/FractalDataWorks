using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for setting (replacing) permissions on a role.
/// All deletes and inserts run in a single database transaction:
/// either everything commits or nothing does.
/// </summary>
public abstract class SetRolePermissionsEndpointBase : Endpoint<SetRolePermissionsRequest, List<PermissionSummaryDto>>
{
    // The domain and implementation a role-permission row is written under. These are values the
    // seed already writes (authz.RolePermission Domain/Implementation = 'RolePermission'), not names
    // this endpoint chooses: a save under any other pair produces a row nothing composes on a read.
    private const string RolePermissionDomain = "RolePermission";
    private const string RolePermissionImplementation = "RolePermission";

    /// <summary>Initializes a new instance of the <see cref="SetRolePermissionsEndpointBase"/> class.</summary>
        private readonly IRolePermissionConfigurationProvider _rolePermissionProvider;
    private readonly IRoleConfigurationProvider _roleProvider;
    private readonly IPermissionConfigurationProvider _permissionProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    private readonly ITenantContext? _tenantContext;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly ITokenRevocationStore _revocations;

    /// <summary>Initializes a new instance of the <see cref="SetRolePermissionsEndpointBase"/> class.</summary>
    protected SetRolePermissionsEndpointBase(ILogger logger, IRolePermissionConfigurationProvider rolePermissionProvider,
        IRoleConfigurationProvider roleProvider,
        IPermissionConfigurationProvider permissionProvider,
        ISystemRoleConfiguration systemRoleConfiguration,
        IUserRoleConfigurationProvider userRoles,
        ITokenRevocationStore revocations,
        ITenantContext? tenantContext = null)
    {
        _userRoles = userRoles;
        _revocations = revocations;
        EndpointLogger = logger;
        _rolePermissionProvider = rolePermissionProvider;
        _roleProvider = roleProvider;
        _permissionProvider = permissionProvider;
        _systemRoleConfiguration = systemRoleConfiguration;
        _tenantContext = tenantContext;
    }


    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:write".
    /// </summary>
    protected virtual string WritePolicy => "settings/role:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/roles/{Name}/permissions");
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SetRolePermissionsRequest req, CancellationToken ct)
    {
        
        AuthorizationEndpointLog.SettingRolePermissions(EndpointLogger, req.Name);

        if (_systemRoleConfiguration.IsSystemRole(req.Name))
        {
            await Send.ResultAsync(TypedResults.Json(new
            {
                errorCode = "SystemRoleImmutable",
                messages = new[] { $"Role '{req.Name}' is a built-in system role and its permissions cannot be modified through this endpoint." }
            }, statusCode: 403)).ConfigureAwait(false);
            return;
        }

        var roleResult = await _roleProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (!roleResult.IsSuccess)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, req.Name,
                roleResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }
        if (roleResult.Value is null)
        {
            AuthorizationEndpointLog.RoleNotFound(EndpointLogger, req.Name);
            await Send.ResultAsync(TypedResults.Json(new { errorCode = "NotFound", messages = new[] { $"roles '{req.Name}' was not found." } }, statusCode: 404)).ConfigureAwait(false);
            return;
        }
        var role = roleResult.Value;
        AuthorizationEndpointLog.RoleResolved(EndpointLogger, req.Name, role.Id);

        // Omitted (null) means the caller did not send a permissions field at all, so the role's
        // current set is left untouched -- reads it back rather than replacing it with nothing. An
        // explicit empty list is still a real request to strip every permission.
        if (req.PermissionNames is null)
        {
            AuthorizationEndpointLog.RolePermissionsLeftUnchanged(EndpointLogger, req.Name);
            await SendUnchangedPermissions(req.Name, role, ct).ConfigureAwait(false);
            return;
        }

        var authContext = Resolve<IAuthenticationContextAccessor>();
        var owningTenant = await MayChange(role, authContext.Current, ct).ConfigureAwait(false);
        if (owningTenant is null)
            return;

        AuthorizationEndpointLog.ReplacingRolePermissions(EndpointLogger, req.Name, req.PermissionNames.Count);

        var resolved = await ResolveRequested(req.Name, req.PermissionNames, ct).ConfigureAwait(false);
        if (resolved is null)
            return;

        var existingMappingsResult = await _rolePermissionProvider
            .Find<IRolePermissionImplementationConfiguration>(rp => rp.RoleId == role.Id, ct).ConfigureAwait(false);
        if (!existingMappingsResult.IsSuccess || existingMappingsResult.Value is null)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, req.Name,
                existingMappingsResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        AuthorizationEndpointLog.ExistingRolePermissionsLoaded(EndpointLogger, req.Name, existingMappingsResult.Value.Count);
        AuthorizationEndpointLog.RolePermissionDelta(EndpointLogger, req.Name,
            existingMappingsResult.Value.Count, resolved.Count);

        if (!await ApplyAndEndSessions(req, role, resolved, existingMappingsResult.Value, authContext, owningTenant.Value, ct).ConfigureAwait(false))
            return;

        await Send.OkAsync(resolved, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the requested permission names against the catalogue, answering the refusal when the catalogue
    /// cannot be read, is empty, or does not hold every name.
    /// </summary>
    /// <returns>The resolved permissions; null when a response has already been sent.</returns>
    private async Task<List<PermissionSummaryDto>?> ResolveRequested(string roleName, IList<string> permissionNames, CancellationToken ct)
    {
        var allPermissionsResult = await _permissionProvider.Get(ct).ConfigureAwait(false);
        if (!allPermissionsResult.IsSuccess || allPermissionsResult.Value is null)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, roleName,
                allPermissionsResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return null;
        }
        AuthorizationEndpointLog.PermissionCatalogLoaded(EndpointLogger, allPermissionsResult.Value.Count);

        // Why Critical and refused: the read SUCCEEDED and returned nothing, so no name can ever
        // resolve. Every set would be refused below with "not found" per name, which reads as a
        // caller mistake and hides that the catalogue itself is missing.
        if (allPermissionsResult.Value.Count == 0)
        {
            AuthorizationEndpointLog.PermissionCatalogEmpty(EndpointLogger, roleName);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return null;
        }

        var (resolved, unresolved) = ResolvePermissions(permissionNames, allPermissionsResult.Value);

        // Why refuse rather than apply what resolved: applying part of a set the caller did not ask
        // for is a silent wrong answer. Refused before the existing mappings are read, so a rejected
        // request touches nothing.
        if (unresolved.Count > 0)
        {
            AuthorizationEndpointLog.PermissionNamesNotResolved(EndpointLogger, roleName,
                unresolved.Count, string.Join(", ", unresolved));
            await Send.ResultAsync(TypedResults.Json(new
            {
                errorCode = "UnknownPermission",
                messages = unresolved
                    .Select(n => $"permission '{n}' was not found.")
                    .ToArray()
            }, statusCode: 400)).ConfigureAwait(false);
            return null;
        }

        return resolved;
    }

    /// <summary>
    /// Decides whether the caller may change what <paramref name="role"/> grants, and answers the refusal when not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A role's grants count only when they are owned by the role's own tenant (see <c>RoleOwnedGrants</c>), and
    /// a role shared by every tenant grants the same in every tenant. So changing a shared role is a platform
    /// decision: only a platform administrator may make it. A tenant's own role is changed only while acting in
    /// that tenant.
    /// </para>
    /// <para>
    /// A role with no owning tenant recorded is refused outright: there is no tenant its grants could be written
    /// for, and none is assumed.
    /// </para>
    /// </remarks>
    /// <returns>The role's owning tenant when the change may go ahead; null when a response has already been sent.</returns>
    private async Task<Guid?> MayChange(IRoleImplementationConfiguration role, IAuthenticationContext? caller, CancellationToken ct)
    {
        if (role.TenantId is null)
        {
            AuthorizationEndpointLog.RoleTenantNotRecorded(EndpointLogger, role.Name);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return null;
        }

        if (!role.IsTenantScoped)
        {
            if (_systemRoleConfiguration.IsPlatformAdmin(User))
                return role.TenantId;

            AuthorizationEndpointLog.SharedRoleChangeRefused(EndpointLogger, role.Name);
            await Send.ResultAsync(TypedResults.Json(new
            {
                errorCode = "SharedRoleChangeRefused",
                messages = new[] { $"Role '{role.Name}' is shared by every tenant; only a platform administrator may change its permissions." }
            }, statusCode: 403)).ConfigureAwait(false);
            return null;
        }

        if (caller?.ActiveTenantId == role.TenantId)
            return role.TenantId;

        AuthorizationEndpointLog.TenantRoleChangeRefused(EndpointLogger, role.Name,
            role.TenantId.Value.ToString("D"),
            caller?.ActiveTenantId is { } acting ? acting.ToString("D") : "(none)");
        await Send.ResultAsync(TypedResults.Json(new
        {
            errorCode = "TenantRoleChangeRefused",
            messages = new[] { $"Role '{role.Name}' belongs to another tenant and cannot be changed from this one." }
        }, statusCode: 403)).ConfigureAwait(false);
        return null;
    }

    /// <summary>
    /// Writes the new permission set, then ends the live sessions of every holder of the role: what
    /// the role grants changed, so no holder may keep a token issued under the old set (FDW-875).
    /// </summary>
    /// <returns>True when both succeeded; false when a response has already been sent.</returns>
    private async Task<bool> ApplyAndEndSessions(
        SetRolePermissionsRequest req,
        IRoleImplementationConfiguration role,
        List<PermissionSummaryDto> resolved,
        IReadOnlyList<IRolePermissionImplementationConfiguration> existing,
        IAuthenticationContextAccessor authContext,
        Guid owningTenant,
        CancellationToken ct)
    {
        var setResult = await SetPermissions(req, role, resolved, existing, authContext, owningTenant, ct).ConfigureAwait(false);
        if (!setResult.IsSuccess)
            return false;

        var ended = await RoleHolderSessions.End(_userRoles, _revocations, role.Id, req.Name, EndpointLogger, ct).ConfigureAwait(false);
        if (ended.IsFailure)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    /// <summary>Sends the role's current permission set unmodified, for an omitted permissions field.</summary>
    private async Task SendUnchangedPermissions(string roleName, IRoleImplementationConfiguration role, CancellationToken ct)
    {
        var currentResult = await _rolePermissionProvider
            .Find<IRolePermissionImplementationConfiguration>(rp => rp.RoleId == role.Id, ct).ConfigureAwait(false);
        if (!currentResult.IsSuccess || currentResult.Value is null)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, roleName, currentResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var currentPermissionsResult = await _permissionProvider.Get(ct).ConfigureAwait(false);
        if (!currentPermissionsResult.IsSuccess || currentPermissionsResult.Value is null)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, roleName, currentPermissionsResult.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var unchanged = currentResult.Value
            .Join(currentPermissionsResult.Value, rp => rp.PermissionId, p => p.Id, (_, p) => new PermissionSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Domain = p.Domain,
                Resource = p.Resource,
                Action = p.Action,
                Scope = p.Scope,
                DisplayName = p.DisplayName,
                Description = p.Description
            }).ToList();

        await Send.OkAsync(unchanged, ct).ConfigureAwait(false);
    }

    // Why both lists: a name that does not resolve is a caller error, not an empty answer. Dropping
    // it silently applied a set the caller never asked for and still reported success, so the
    // unresolved names come back with the resolved ones and the handler refuses the whole set.
    private (List<PermissionSummaryDto> Resolved, List<string> Unresolved) ResolvePermissions(
        IList<string> permissionNames,
        IReadOnlyList<IPermissionImplementationConfiguration> allPermissions)
    {
        var orgPrefix = _tenantContext?.CurrentTenant?.OrgPrefix;
        var tenantPrefix = string.IsNullOrEmpty(orgPrefix) ? null : orgPrefix + ":";
        var resolved = new List<PermissionSummaryDto>();
        var unresolved = new List<string>();

        foreach (var rawName in permissionNames)
        {
            var permName = StripTenantPrefix(rawName, tenantPrefix);
            if (permName is null)
            {
                unresolved.Add(rawName);
                continue;
            }

            var perm = allPermissions.FirstOrDefault(p => string.Equals(p.Name, permName, StringComparison.OrdinalIgnoreCase));
            if (perm is null)
            {
                unresolved.Add(rawName);
                continue;
            }

            resolved.Add(new PermissionSummaryDto
            {
                Id = perm.Id,
                Name = perm.Name,
                Domain = perm.Domain,
                Resource = perm.Resource,
                Action = perm.Action,
                Scope = perm.Scope,
                DisplayName = perm.DisplayName,
                Description = perm.Description
            });
        }

        return (resolved, unresolved);
    }

    /// <summary>
    /// Replaces the role's permission set in one operation.
    /// </summary>
    /// <remarks>
    /// The provider applies the whole set in a single unit of work, so the role ends up holding the
    /// set it was given or the set it already had, never a mixture. This used to delete and save a
    /// row at a time, and a comment here predicted that making Save and Delete transactional would
    /// fix it on its own. It does not: each row would still commit separately, which is what left a
    /// half-applied set behind.
    /// </remarks>
    private async Task<IGenericResult> SetPermissions(
        SetRolePermissionsRequest req,
        IRoleImplementationConfiguration role,
        List<PermissionSummaryDto> resolved,
        IReadOnlyList<IRolePermissionImplementationConfiguration> existingMappings,
        IAuthenticationContextAccessor authContext,
        Guid owningTenant,
        CancellationToken ct)
    {
        var assignedBy = authContext.Current?.UserId;
        var mappings = resolved.ConvertAll(perm => (IRolePermissionImplementationConfiguration)new RolePermissionImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            // The seed writes this name as {roleName}:{permissionName} (r.Name + ':' + p.Name);
            // minting it from ids instead produces a row the seed's own idempotence check misses.
            Name = $"{role.Name}:{perm.Name}",
            RoleId = role.Id,
            PermissionId = perm.Id,
            AssignedBy = assignedBy,
            AssignedAt = DateTimeOffset.UtcNow
        });

        // Why elevated, for the role's tenant: a grant counts only when it is owned by the role's own tenant,
        // and the store records the tenant a write runs in. A platform administrator changing a shared role acts
        // in the global tenant, so without this the grants would belong to global and count nowhere. MayChange
        // has already decided the caller may make this write; the scope decides only whose rows it writes.
        IGenericResult replaced;
        using (new SystemAuthenticationContextScope(authContext, owningTenant))
        {
            replaced = await _rolePermissionProvider.ReplacePermissions(
                existingMappings.Select(m => m.Id).ToList(), mappings, ct).ConfigureAwait(false);
        }
        if (!replaced.IsSuccess)
        {
            AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, req.Name, replaced.CurrentMessage);
            OnPermissionUpdateFailed(req.Name);
            await Send.ResponseAsync(new List<PermissionSummaryDto>(), 400, ct).ConfigureAwait(false);
            return replaced;
        }

        AuthorizationEndpointLog.RolePermissionsUpdated(EndpointLogger, req.Name, resolved.Count);
        return GenericResult.Success();
    }

    protected virtual void OnPermissionUpdateFailed(string roleName)
    {
    }

    private static string? StripTenantPrefix(string rawName, string? tenantPrefix)
    {
        if (tenantPrefix is not null
            && rawName.StartsWith(tenantPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return rawName.Substring(tenantPrefix.Length);
        }

        // If the tenant has a prefix configured AND the inbound name carries a 3-segment
        // shape ("something:resource:action"), the leading segment is a foreign prefix.
        if (tenantPrefix is not null
            && rawName.Contains(':', StringComparison.Ordinal)
            && rawName.Split(':').Length > 2)
        {
            return null;
        }

        // No tenant prefix configured, or caller already sent the bare "{resource}:{action}".
        return rawName;
    }
}
