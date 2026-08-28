using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for getting permissions assigned to a specific role.
/// </summary>
public abstract class GetRolePermissionsEndpointBase : Endpoint<GetRoleRequest, List<PermissionSummaryDto>>
{
    private readonly RoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected GetRolePermissionsEndpointBase(RoleConfigurationProvider roleProvider)
    {
        _roleProvider = roleProvider;
    }

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected RoleConfigurationProvider RoleProvider => _roleProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "settings/role:read".
    /// </summary>
    protected virtual string ReadPolicy => "settings/role:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/roles/{Name}/permissions");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(GetRoleRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        AuthorizationEndpointLog.GettingRolePermissions(EndpointLogger, req.Name);

        var role = Guid.TryParse(req.Name, out var roleId)
            ? await _roleProvider.GetRole(roleId, ct).ConfigureAwait(false)
            : await _roleProvider.GetRole(req.Name, ct).ConfigureAwait(false);
        if (role is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var rolePermissions = await _roleProvider.GetRolePermissions(role.Id, ct).ConfigureAwait(false);
        var permissions = await _roleProvider.GetPermissions(ct).ConfigureAwait(false);

        var orgPrefix = Resolve<ITenantContext>()?.CurrentTenant?.OrgPrefix;
        var prefix = string.IsNullOrEmpty(orgPrefix) ? null : orgPrefix + ":";

        var response = rolePermissions
            .Select(rp => permissions.FirstOrDefault(p => p.Id == rp.PermissionId))
            .Where(p => p is not null)
            .Select(p => new PermissionSummaryDto
            {
                Id = p!.Id,
                Name = prefix is null ? p.Name : prefix + p.Name,
                Domain = p.Domain,
                Resource = p.Resource,
                Action = p.Action,
                Scope = p.Scope,
                DisplayName = p.DisplayName,
                Description = p.Description
            })
            .ToList();

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}
