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
    /// <summary>Initializes a new instance of the <see cref="GetRolePermissionsEndpointBase"/> class.</summary>
        private readonly IAuthorizationProvider _authorizationProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    private readonly ITenantContext? _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="GetRolePermissionsEndpointBase"/> class.</summary>
    protected GetRolePermissionsEndpointBase(ILogger logger, IAuthorizationProvider authorizationProvider, ITenantContext? tenantContext = null)
    {
        EndpointLogger = logger;
        _authorizationProvider = authorizationProvider;
        _tenantContext = tenantContext;
    }


    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected IAuthorizationProvider AuthorizationProvider => _authorizationProvider;

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
        
        AuthorizationEndpointLog.GettingRolePermissions(EndpointLogger, req.Name);

        var role = Guid.TryParse(req.Name, out var roleId)
            ? await _authorizationProvider.GetRole(roleId, ct).ConfigureAwait(false)
            : await _authorizationProvider.GetRole(req.Name, ct).ConfigureAwait(false);
        if (role is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var rolePermissions = await _authorizationProvider.GetRolePermissions(role.Id, ct).ConfigureAwait(false);
        var permissions = await _authorizationProvider.GetPermissions(ct).ConfigureAwait(false);

        var orgPrefix = _tenantContext?.CurrentTenant?.OrgPrefix;
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
