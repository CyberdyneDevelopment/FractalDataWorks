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
    private readonly ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand> _rolePermissionProvider;
    private readonly RoleConfigurationProvider _roleProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected SetRolePermissionsEndpointBase(
        ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand> rolePermissionProvider,
        RoleConfigurationProvider roleProvider,
        ISystemRoleConfiguration systemRoleConfiguration)
    {
        _rolePermissionProvider = rolePermissionProvider;
        _roleProvider = roleProvider;
        _systemRoleConfiguration = systemRoleConfiguration;
    }

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected RoleConfigurationProvider RoleProvider => _roleProvider;

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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

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

        var role = await _roleProvider.GetRole(req.Name, ct).ConfigureAwait(false);
        if (role is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new { errorCode = "NotFound", messages = new[] { $"roles '{req.Name}' was not found." } }, ct).ConfigureAwait(false);
            return;
        }

        var allPermissions = await _roleProvider.GetPermissions(ct).ConfigureAwait(false);
        var resolved = ResolvePermissions(req, allPermissions);

        var existingMappings = await _roleProvider.GetRolePermissions(role.Id, ct).ConfigureAwait(false);

        var setResult = await SetPermissionsAtomically(req, role, resolved, existingMappings, ct).ConfigureAwait(false);
        if (!setResult.IsSuccess)
            return;

        await Send.OkAsync(resolved, ct).ConfigureAwait(false);
    }

    private List<PermissionSummaryDto> ResolvePermissions(
        SetRolePermissionsRequest req,
        IReadOnlyList<PermissionConfiguration> allPermissions)
    {
        var orgPrefix = Resolve<ITenantContext>()?.CurrentTenant?.OrgPrefix;
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

    private async Task<IGenericResult> SetPermissionsAtomically(
        SetRolePermissionsRequest req,
        RoleConfiguration role,
        List<PermissionSummaryDto> resolved,
        IReadOnlyList<RolePermissionConfiguration> existingMappings,
        CancellationToken ct)
    {
        var txnResult = await _rolePermissionProvider.BeginTransaction(ct).ConfigureAwait(false);
        if (!txnResult.IsSuccess || txnResult.Value == null)
        {
            var reason = txnResult.CurrentMessage ?? "Transaction could not be opened";
            var msg = AuthorizationEndpointLog.TransactionOpenFailed(EndpointLogger, req.Name, reason);
            await Send.ResponseAsync(new List<PermissionSummaryDto>(), 500, ct).ConfigureAwait(false);
            return GenericResult.Failure(msg);
        }

        var txn = txnResult.Value;
        try
        {
            var deleteResult = await DeleteExistingMappings(req, existingMappings, txn, ct).ConfigureAwait(false);
            if (!deleteResult.IsSuccess)
                return deleteResult;

            var saveResult = await SaveNewMappings(req, role, resolved, txn, ct).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
                return saveResult;

            AuthorizationEndpointLog.RolePermissionsUpdated(EndpointLogger, req.Name, resolved.Count);

            var commitResult = await txn.Commit(ct).ConfigureAwait(false);
            if (!commitResult.IsSuccess)
            {
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, req.Name,
                    commitResult.CurrentMessage ?? "Commit failed");
                await Send.ResponseAsync(new List<PermissionSummaryDto>(), 500, ct).ConfigureAwait(false);
                return commitResult;
            }

            _rolePermissionProvider.InvalidateCache();

            return GenericResult.Success();
        }
        finally
        {
            await txn.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<IGenericResult> DeleteExistingMappings(
        SetRolePermissionsRequest req,
        IReadOnlyList<RolePermissionConfiguration> existingMappings,
        IDataGatewayTransaction txn,
        CancellationToken ct)
    {
        foreach (var existing in existingMappings)
        {
            var deleteResult = await _rolePermissionProvider.DeleteInTransaction(existing.Id, txn, ct).ConfigureAwait(false);
            if (!deleteResult.IsSuccess)
            {
                var rollbackResult = await txn.Rollback(ct).ConfigureAwait(false);
                if (!rollbackResult.IsSuccess)
                    AuthorizationEndpointLog.RollbackFailed(EndpointLogger, req.Name, rollbackResult.CurrentMessage);
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, req.Name,
                    deleteResult.CurrentMessage ?? "Permission delete failed");
                OnPermissionUpdateFailed(req.Name);
                await Send.ResponseAsync(new List<PermissionSummaryDto>(), 400, ct).ConfigureAwait(false);
                return deleteResult;
            }
        }
        return GenericResult.Success();
    }

    private async Task<IGenericResult> SaveNewMappings(
        SetRolePermissionsRequest req,
        RoleConfiguration role,
        List<PermissionSummaryDto> resolved,
        IDataGatewayTransaction txn,
        CancellationToken ct)
    {
        foreach (var perm in resolved)
        {
            var mapping = new RolePermissionConfiguration
            {
                Id = Guid.NewGuid(),
                Name = $"{role.Id}:{perm.Id}",
                RoleId = role.Id,
                PermissionId = perm.Id,
                AssignedAt = DateTimeOffset.UtcNow
            };

            var saveResult = await _rolePermissionProvider.SaveInTransaction(mapping, txn, ct).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
            {
                var rollbackResult = await txn.Rollback(ct).ConfigureAwait(false);
                if (!rollbackResult.IsSuccess)
                    AuthorizationEndpointLog.RollbackFailed(EndpointLogger, req.Name, rollbackResult.CurrentMessage);
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, req.Name,
                    saveResult.CurrentMessage ?? "Permission save failed");
                OnPermissionUpdateFailed(req.Name);
                await Send.ResponseAsync(new List<PermissionSummaryDto>(), 400, ct).ConfigureAwait(false);
                return saveResult;
            }
        }
        return GenericResult.Success();
    }

    /// <summary>
    /// Called when a permission update fails. Override for custom logging.
    /// </summary>
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
