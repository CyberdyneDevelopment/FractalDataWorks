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
/// Generic base endpoint for listing the people in an organization and their roles (FDW-1593).
/// </summary>
public abstract class ListOrganizationMembersEndpointBase : Endpoint<OrganizationRequest, OrganizationMembersResponse>
{
    private readonly IOrganizationMembers _members;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationMembersEndpointBase"/> class.</summary>
    protected ListOrganizationMembersEndpointBase(ILogger logger, IOrganizationMembers members)
    {
        EndpointLogger = logger;
        _members = members;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/members");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRequest req, CancellationToken ct)
    {
        var listed = await _members.List(req.OrganizationId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new OrganizationMembersResponse
        {
            Members = [.. listed.Value.Select(member => new OrganizationMemberResponse { UserId = member.UserId, RoleName = member.RoleName })],
        }, ct).ConfigureAwait(false);
    }
}
