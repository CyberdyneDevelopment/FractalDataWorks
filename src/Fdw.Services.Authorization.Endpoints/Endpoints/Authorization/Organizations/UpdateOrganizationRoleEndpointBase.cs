using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for changing the description and permissions of a role of an organization (FDW-1593). Everyone who holds the
/// role is refreshed with the permissions it carries from now.
/// </summary>
public abstract class UpdateOrganizationRoleEndpointBase : Endpoint<OrganizationRoleRequest, OrganizationRoleResponse>
{
    private readonly IOrganizationRoles _roles;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="UpdateOrganizationRoleEndpointBase"/> class.</summary>
    protected UpdateOrganizationRoleEndpointBase(ILogger logger, IOrganizationRoles roles, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _roles = roles;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/organizations/{OrganizationId}/roles/{RoleName}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRoleRequest req, CancellationToken ct)
    {
        var before = await _roles.Get(req.OrganizationId, req.RoleName, ct).ConfigureAwait(false);
        if (before.IsFailure || before.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(before, HttpContext)).ConfigureAwait(false);
            return;
        }

        var changed = await _roles.Redefine(req.OrganizationId, req.RoleName, req.Description, [.. req.Permissions], ct).ConfigureAwait(false);
        if (changed.IsFailure || changed.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(changed, HttpContext)).ConfigureAwait(false);
            return;
        }

        var role = changed.Value;
        var audited = await OrganizationChangeAudit.Role(_audit, "OrganizationRoleChanged", req.OrganizationId, role.Name,
            OrganizationChangeAudit.Describe(before.Value.Description, before.Value.Permissions),
            OrganizationChangeAudit.Describe(role.Description, role.Permissions), ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.OrganizationChangeNotAudited(EndpointLogger, "change", role.Name, req.OrganizationId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new OrganizationRoleResponse
        {
            Name = role.Name, Description = role.Description, IsBuiltIn = role.IsBuiltIn, Permissions = [.. role.Permissions],
        }, ct).ConfigureAwait(false);
    }
}
