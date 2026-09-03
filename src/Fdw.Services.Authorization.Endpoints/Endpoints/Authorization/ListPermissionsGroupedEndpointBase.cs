using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for listing permissions grouped by resource.
/// </summary>
public abstract class ListPermissionsGroupedEndpointBase : EndpointWithoutRequest<List<PermissionGroupResponse>>
{
    /// <summary>Initializes a new instance of the <see cref="ListPermissionsGroupedEndpointBase"/> class.</summary>
        private readonly RoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListPermissionsGroupedEndpointBase"/> class.</summary>
    protected ListPermissionsGroupedEndpointBase(ILogger logger, RoleConfigurationProvider roleProvider)
    {
        EndpointLogger = logger;
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
        Get("/permissions/grouped");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        
        AuthorizationEndpointLog.ListingPermissions(EndpointLogger);

        var allPermissions = await _roleProvider.GetPermissions(ct).ConfigureAwait(false);

        var grouped = allPermissions
            .GroupBy(p => p.Domain, StringComparer.Ordinal)
            .Select(g => new PermissionGroupResponse
            {
                Domain = g.Key,
                Permissions = g.Select(p => new PermissionSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Domain = p.Domain,
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope,
                    DisplayName = p.DisplayName,
                    Description = p.Description
                }).ToList()
            })
            .OrderBy(g => g.Domain, StringComparer.Ordinal)
            .ToList();

        AuthorizationEndpointLog.ListedPermissions(EndpointLogger, grouped.Sum(g => g.Permissions.Count));

        await Send.OkAsync(grouped, ct).ConfigureAwait(false);
    }
}
