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
    private readonly IAuthorizationProvider _authorizationProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    private readonly ITenantContext? _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="SetRolePermissionsEndpointBase"/> class.</summary>
    protected SetRolePermissionsEndpointBase(ILogger logger, IRolePermissionConfigurationProvider rolePermissionProvider,
        IAuthorizationProvider authorizationProvider,
        ISystemRoleConfiguration systemRoleConfiguration,
        ITenantContext? tenantContext = null)
    {
        EndpointLogger = logger;
        _rolePermissionProvider = rolePermissionProvider;
        _authorizationProvider = authorizationProvider;
        _systemRoleConfiguration = systemRoleConfiguration;
        _tenantContext = tenantContext;
    }


    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected IAuthorizationProvider AuthorizationProvider => _authorizationProvider;

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
            HttpContext.Response.StatusCode = 403;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "SystemRoleImmutable",
                messages = new[] { $"Role '{req.Name}' is a built-in system role and its permissions cannot be modified through this endpoint." }
            }, ct).ConfigureAwait(false);
            return;
        }

        var role = await _authorizationProvider.GetRole(req.Name, ct).ConfigureAwait(false);
        if (role is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new { errorCode = "NotFound", messages = new[] { $"roles '{req.Name}' was not found." } }, ct).ConfigureAwait(false);
            return;
        }

        var allPermissions = await _authorizationProvider.GetPermissions(ct).ConfigureAwait(false);
        var resolved = ResolvePermissions(req, allPermissions);

        var existingMappings = await _authorizationProvider.GetRolePermissions(role.Id, ct).ConfigureAwait(false);

        var setResult = await SetPermissions(req, role, resolved, existingMappings, ct).ConfigureAwait(false);
        if (!setResult.IsSuccess)
            return;

        await Send.OkAsync(resolved, ct).ConfigureAwait(false);
    }

    private List<PermissionSummaryDto> ResolvePermissions(
        SetRolePermissionsRequest req,
        IReadOnlyList<PermissionImplementationConfiguration> allPermissions)
    {
        var orgPrefix = _tenantContext?.CurrentTenant?.OrgPrefix;
        var tenantPrefix = string.IsNullOrEmpty(orgPrefix) ? null : orgPrefix + ":";
        var resolved = new List<PermissionSummaryDto>();

        foreach (var rawName in req.PermissionNames)
        {
            var permName = StripTenantPrefix(rawName, tenantPrefix);
            if (permName is null)
                continue;

            var perm = allPermissions.FirstOrDefault(p => string.Equals(p.Name, permName, StringComparison.OrdinalIgnoreCase));
            if (perm is null)
                continue;

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

        return resolved;
    }

    /// <summary>
    /// Replaces the role's permission set: the existing mappings go, then the resolved ones are written.
    /// </summary>
    /// <remarks>
    /// NOT atomic. Every delete and every save is its own write, so a failure part-way through leaves
    /// the role holding some of the old set and none or some of the new, and the response says which
    /// step failed rather than pretending nothing happened. This is deliberate and temporary: a
    /// transaction scope on the provider is the open piece of work, and when Save and Delete become
    /// transactional this method inherits it without changing.
    /// </remarks>
    private async Task<IGenericResult> SetPermissions(
        SetRolePermissionsRequest req,
        RoleImplementationConfiguration role,
        List<PermissionSummaryDto> resolved,
        IReadOnlyList<RolePermissionImplementationConfiguration> existingMappings,
        CancellationToken ct)
    {
        foreach (var existing in existingMappings)
        {
            var deleteResult = await _rolePermissionProvider.Delete(existing.Id, ct).ConfigureAwait(false);
            if (!deleteResult.IsSuccess)
            {
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, req.Name,
                    deleteResult.CurrentMessage ?? "Permission delete failed");
                OnPermissionUpdateFailed(req.Name);
                await Send.ResponseAsync(new List<PermissionSummaryDto>(), 400, ct).ConfigureAwait(false);
                return deleteResult;
            }
        }

        foreach (var perm in resolved)
        {
            var mapping = new RolePermissionImplementationConfiguration
            {
                Id = Guid.NewGuid(),
                // The seed writes this name as {roleName}:{permissionName} (r.Name + ':' + p.Name);
                // minting it from ids instead produces a row the seed's own idempotence check misses.
                Name = $"{role.Name}:{perm.Name}",
                RoleId = role.Id,
                PermissionId = perm.Id,
                AssignedAt = DateTimeOffset.UtcNow
            };

            var saveResult = await _rolePermissionProvider
                .Save(mapping, RolePermissionDomain, RolePermissionImplementation, mapping.Name, ct)
                .ConfigureAwait(false);
            if (!saveResult.IsSuccess)
            {
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, req.Name,
                    saveResult.CurrentMessage ?? "Permission save failed");
                OnPermissionUpdateFailed(req.Name);
                await Send.ResponseAsync(new List<PermissionSummaryDto>(), 400, ct).ConfigureAwait(false);
                return saveResult;
            }
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
