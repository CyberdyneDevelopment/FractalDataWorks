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
/// Generic base endpoint for defining a role in an organization (FDW-1593).
/// </summary>
/// <remarks>
/// The role is checked and saved by <see cref="IOrganizationRoles"/>: the caller must hold the right at the organization's scope, the role carries at least
/// one permission and only permissions the caller holds. The change is then recorded in the audit.
/// </remarks>
public abstract class CreateOrganizationRoleEndpointBase : Endpoint<OrganizationRoleRequest, OrganizationRoleResponse>
{
    private readonly IOrganizationRoles _roles;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="CreateOrganizationRoleEndpointBase"/> class.</summary>
    protected CreateOrganizationRoleEndpointBase(ILogger logger, IOrganizationRoles roles, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _roles = roles;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations/{OrganizationId}/roles");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRoleRequest req, CancellationToken ct)
    {
        var defined = await _roles.Define(req.OrganizationId, req.RoleName, req.Description, [.. req.Permissions], ct).ConfigureAwait(false);
        if (defined.IsFailure || defined.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(defined, HttpContext)).ConfigureAwait(false);
            return;
        }

        var role = defined.Value;
        var audited = await OrganizationChangeAudit.Role(_audit, "OrganizationRoleDefined", req.OrganizationId, role.Name, null,
            OrganizationChangeAudit.Describe(role.Description, role.Permissions), ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.OrganizationChangeNotAudited(EndpointLogger, "definition", role.Name, req.OrganizationId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(new OrganizationRoleResponse
        {
            Name = role.Name, Description = role.Description, IsBuiltIn = role.IsBuiltIn, Permissions = [.. role.Permissions],
        }, 201, ct).ConfigureAwait(false);
    }
}
