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
/// Generic base endpoint for listing the roles an organization defines (FDW-1593).
/// </summary>
public abstract class ListOrganizationRolesEndpointBase : Endpoint<OrganizationRequest, OrganizationRolesResponse>
{
    private readonly IOrganizationRoles _roles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationRolesEndpointBase"/> class.</summary>
    protected ListOrganizationRolesEndpointBase(ILogger logger, IOrganizationRoles roles)
    {
        EndpointLogger = logger;
        _roles = roles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/roles");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRequest req, CancellationToken ct)
    {
        var listed = await _roles.List(req.OrganizationId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new OrganizationRolesResponse
        {
            Roles = [.. listed.Value.Select(role => new OrganizationRoleResponse
            {
                Name = role.Name, Description = role.Description, IsBuiltIn = role.IsBuiltIn, Permissions = [.. role.Permissions],
            })],
        }, ct).ConfigureAwait(false);
    }
}
