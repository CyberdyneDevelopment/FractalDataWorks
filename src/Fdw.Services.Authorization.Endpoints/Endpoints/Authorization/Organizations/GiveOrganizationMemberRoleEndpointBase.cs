using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for giving a person a role in an organization, adding them or changing the role they hold there (FDW-1593).
/// </summary>
/// <remarks>The person must already have an account. A person holds one role in an organization; the one given replaces the old.</remarks>
public abstract class GiveOrganizationMemberRoleEndpointBase : Endpoint<OrganizationMemberRequest, OrganizationMemberResponse>
{
    private readonly IOrganizationMembers _members;
    private readonly IUserConfigurationProvider _users;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="GiveOrganizationMemberRoleEndpointBase"/> class.</summary>
    protected GiveOrganizationMemberRoleEndpointBase(ILogger logger, IOrganizationMembers members, IUserConfigurationProvider users, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _members = members;
        _users = users;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/organizations/{OrganizationId}/members/{UserId}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationMemberRequest req, CancellationToken ct)
    {
        var person = await _users.Get(req.UserId, ct).ConfigureAwait(false);
        if (!person.IsSuccess || person.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var given = await _members.Give(req.OrganizationId, req.UserId, req.RoleName, ct).ConfigureAwait(false);
        if (given.IsFailure || given.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(given, HttpContext)).ConfigureAwait(false);
            return;
        }

        var change = given.Value;
        var audited = await OrganizationChangeAudit.Member(_audit,
            change.PreviousRole is null ? "OrganizationMemberAdded" : "OrganizationMemberRoleChanged",
            req.OrganizationId, req.UserId, change.PreviousRole, change.Role, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.OrganizationChangeNotAudited(EndpointLogger, "membership", req.UserId.ToString(), req.OrganizationId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new OrganizationMemberResponse { UserId = req.UserId, RoleName = change.Role }, ct).ConfigureAwait(false);
    }
}
