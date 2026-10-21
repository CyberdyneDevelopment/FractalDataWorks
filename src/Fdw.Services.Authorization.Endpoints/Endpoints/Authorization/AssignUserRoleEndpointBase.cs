using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Users;
using Fdw.Services.Users.Clients.Models;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for assigning a role to a user.
/// The role assignment is executed in a transaction for atomicity.
/// </summary>
public abstract class AssignUserRoleEndpointBase : Endpoint<AssignRoleRequest, UserRolesResponse>
{
    /// <summary>Initializes a new instance of the <see cref="AssignUserRoleEndpointBase"/> class.</summary>
        private readonly IRoleConfigurationProvider _roleProvider;
    private readonly IUserRoleConfigurationProvider _userRoleProvider;
    private readonly IUserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="AssignUserRoleEndpointBase"/> class.</summary>
    protected AssignUserRoleEndpointBase(ILogger logger, IRoleConfigurationProvider roleProvider,
        IUserRoleConfigurationProvider userRoleProvider,
        IUserConfigurationProvider userProvider)
    {
        EndpointLogger = logger;
        _roleProvider = roleProvider;
        _userRoleProvider = userRoleProvider;
        _userProvider = userProvider;
    }


    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected IUserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the user-role configuration provider.
    /// </summary>
    protected IUserRoleConfigurationProvider UserRoleProvider => _userRoleProvider;

    /// <summary>
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:write".
    /// </summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/users/{IdOrName}/roles");
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(AssignRoleRequest req, CancellationToken ct)
    {
        
        // The route takes an id OR a name; the domain provider answers each by its own overload.
        var lookup = Guid.TryParse(req.IdOrName, out var parsedUserId)
            ? await _userProvider.Get(parsedUserId, ct).ConfigureAwait(false)
            : await _userProvider.Get(req.IdOrName, ct).ConfigureAwait(false);
        if (!lookup.IsSuccess || lookup.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var userId = lookup.Value.Id;
        var userIdString = userId.ToString();

        // A role is held per tenant (FDW-825) and is granted in the tenant the caller is acting in --
        // the one their token was issued for. RLS enforces the same thing on the write: a tenant
        // admin cannot put a row in any other tenant.
        if (!Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
        {
            AuthorizationEndpointLog.RoleChangeWithoutTenant(EndpointLogger, userIdString);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthorizationEndpointLog.AssigningUserRole(EndpointLogger, req.RoleName, userIdString);

        try
        {
            await AssignRole(req.RoleName, userId, tenantId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthorizationEndpointLog.OperationFailed(EndpointLogger, ex, "assign role", userIdString);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
        }
    }

    // Resolves the role, checks the caller may give it, grants it, and answers with the roles the user holds.
    private async Task AssignRole(string roleName, Guid userId, Guid tenantId, CancellationToken ct)
    {
        var roleResult = await _roleProvider.Get(roleName, ct).ConfigureAwait(false);
        if (!roleResult.IsSuccess)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, roleName,
                roleResult.CurrentMessage);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }
        if (roleResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }
        var role = roleResult.Value;

        // Why before anything is written: the caller may give only what they hold themselves (FDW-1577), and a
        // refusal must leave the user's roles untouched.
        var allowed = await Resolve<IGrantRule>().CanGive([role.Name], tenantId, ct).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(allowed, HttpContext)).ConfigureAwait(false);
            return;
        }

        var granted = await Resolve<IUserRoleAssignment>().Assign(userId, role.Name, tenantId, ct).ConfigureAwait(false);
        if (granted.IsFailure)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(granted, HttpContext)).ConfigureAwait(false);
            return;
        }

        await SendRoles(roleName, role.Name, userId, tenantId, ct).ConfigureAwait(false);
    }

    // Records the assignment in the audit trail and answers with every role the user holds in the tenant.
    private async Task SendRoles(string requestedRoleName, string roleName, Guid userId, Guid tenantId, CancellationToken ct)
    {
        var userIdString = userId.ToString();
        var allRolesResult = await _roleProvider.Get(ct).ConfigureAwait(false);
        if (!allRolesResult.IsSuccess || allRolesResult.Value is null)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, userIdString,
                allRolesResult.CurrentMessage);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }
        var allRoles = allRolesResult.Value;
        var userRolesResult = await _userRoleProvider
            .Find<UserRoleImplementationConfiguration>(
                // authz.UserRoleConfiguration.UserId is a Guid stored as VARCHAR, so comparing
                // the TEXT makes the match depend on how each side happened to spell it. Both
                // sides are parsed and compared as the Guids they are (FDW-532).
                // The roles held in THIS tenant: a caller acting in the global tenant can read every
                // tenant's assignments, and a role held elsewhere is not one this change affected.
                ur => Guid.TryParse(ur.UserId, out var assigned) && assigned == userId && ur.TenantId == tenantId, ct)
            .ConfigureAwait(false);
        if (!userRolesResult.IsSuccess || userRolesResult.Value is null)
        {
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }

        var roles = userRolesResult.Value
            .Select(ur => allRoles.FirstOrDefault(r => r.Id == ur.RoleId)?.Name)
            .Where(name => name is not null)
            .ToList();

        var audited = await RoleChangeAudit.RecordUserRole(Resolve<IAdministrativeAudit>(), User, "RoleAssigned", userId,
            roleName, null, roleName, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.RoleChangeNotAudited(EndpointLogger, "assignment", roleName, userIdString,
                audited.CurrentMessage);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }

        AuthorizationEndpointLog.UserRoleAssigned(EndpointLogger, requestedRoleName, userIdString);

        await Send.OkAsync(new UserRolesResponse
        {
            UserId = userId,
            Roles = roles!
        }, ct).ConfigureAwait(false);
    }
}
