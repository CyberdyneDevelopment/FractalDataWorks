using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users.Clients.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Lists every user's role assignments in one read.
/// </summary>
/// <remarks>
/// <para>
/// The users list cannot answer this. `UserRoleConfiguration` carries a `RoleId` rather than a role
/// name, so resolving names needs the role provider — and the dependency runs authorization to
/// users, not the other way round. A users-domain response carrying role names would either invert
/// that or, as it did, ship a field nothing ever filled: `GET /users` returned an empty roles array
/// for every user, including one holding Admin, while the per-user route returned the real answer
/// at the same moment.
/// </para>
/// <para>
/// One read of the assignments and one of the roles, joined here — not a request per row. That is
/// the whole reason this exists rather than callers looping over the per-user route.
/// </para>
/// </remarks>
public abstract class ListAllUserRolesEndpointBase : EndpointWithoutRequest<AllUserRolesResponse>
{
    private readonly RoleConfigurationProvider _roleProvider;
    private readonly UserRoleConfigurationProvider _userRoleProvider;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListAllUserRolesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleProvider">Resolves role ids to names.</param>
    /// <param name="userRoleProvider">Reads the assignments.</param>
    protected ListAllUserRolesEndpointBase(
        ILogger logger,
        RoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider)
    {
        EndpointLogger = logger;
        _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));
        _userRoleProvider = userRoleProvider ?? throw new ArgumentNullException(nameof(userRoleProvider));
    }

    /// <summary>Gets the RBAC policy required by this endpoint. Defaults to "users:read".</summary>
    protected virtual string ReadPolicy => "users:read";

    /// <inheritdoc />
    public override void Configure()
    {
        // Why not /users/roles: that is the same shape as GET /users/{IdOrName}, and would resolve
        // only because a literal segment outranks a parameter. Correctness that depends on route
        // precedence is correctness a reader cannot see.
        Get("/user-roles");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (auth, summary, etc.).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var assignments = await _userRoleProvider.Get(ct).ConfigureAwait(false);
        if (!assignments.IsSuccess || assignments.Value is null)
        {
            // Why this refuses: an empty list would say "nobody holds any role", which is what the
            // Users screen printed for a year while every user held one. A read that failed and a
            // store that is genuinely empty are opposite facts.
            AuthorizationEndpointLog.UserRoleAssignmentsUnreadable(
                EndpointLogger, assignments.CurrentMessage ?? "no value returned");
            await Send.ResponseAsync(
                new AllUserRolesResponse(), StatusCodes.Status500InternalServerError, ct).ConfigureAwait(false);
            return;
        }

        var roles = await _roleProvider.GetAllRoles(ct).ConfigureAwait(false);

        var items = assignments.Value
            .Where(a => a.IsCurrent && !a.IsDeleted)
            .GroupBy(a => a.UserId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                g.Key,
                Names = g.Select(a => roles.FirstOrDefault(r => r.Id == a.RoleId)?.Name)
                          .Where(n => n is not null)
                          .Select(n => n!)
                          .OrderBy(n => n, StringComparer.Ordinal)
                          .ToList(),
            })
            // An assignment whose role no longer resolves leaves the user with nothing to report,
            // and a user reported with an empty list reads as "has no roles" rather than "holds a
            // role this deployment can no longer name".
            .Where(x => x.Names.Count > 0 && Guid.TryParse(x.Key, out _))
            .Select(x => new UserRolesResponse { UserId = Guid.Parse(x.Key), Roles = x.Names })
            .ToList();

        AuthorizationEndpointLog.UserRoleAssignmentsListed(EndpointLogger, items.Count, roles.Count);

        await Send.OkAsync(new AllUserRolesResponse { Items = items }, ct).ConfigureAwait(false);
    }
}
