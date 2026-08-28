using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for listing all permissions.
/// </summary>
public abstract class ListPermissionsEndpointBase : EndpointWithoutRequest<List<PermissionSummaryDto>>
{
    private readonly RoleConfigurationProvider _roleProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected ListPermissionsEndpointBase(RoleConfigurationProvider roleProvider)
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
        Get("/permissions");
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        AuthorizationEndpointLog.ListingPermissions(EndpointLogger);

        var allPermissions = await _roleProvider.GetPermissions(ct).ConfigureAwait(false);

        var orgPrefix = Resolve<ITenantContext>()?.CurrentTenant?.OrgPrefix;
        var prefix = string.IsNullOrEmpty(orgPrefix) ? null : orgPrefix + ":";

        var permissions = allPermissions
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => MapToSummary(p, prefix))
            .ToList();

        AuthorizationEndpointLog.ListedPermissions(EndpointLogger, permissions.Count);

        await Send.OkAsync(permissions, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a <see cref="PermissionConfiguration"/> to a summary DTO. Override for custom mapping.
    /// </summary>
    /// <param name="permission">The permission row.</param>
    /// <param name="orgPrefix">The current tenant's OrgPrefix already followed by ':' (e.g. "acme:"),
    /// or null if no prefix should be applied.</param>
    protected virtual PermissionSummaryDto MapToSummary(PermissionConfiguration permission, string? orgPrefix)
    {
        return new PermissionSummaryDto
        {
            Id = permission.Id,
            Name = orgPrefix is null ? permission.Name : orgPrefix + permission.Name,
            Domain = permission.Domain,
            Resource = permission.Resource,
            Action = permission.Action,
            Scope = permission.Scope,
            DisplayName = permission.DisplayName,
            Description = permission.Description,
            SortOrder = permission.SortOrder
        };
    }
}
