using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.AspNetCore.Http;
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
    // The domain and implementation a user-role row is written under. These are values the seed
    // already writes (authz.UserRole Domain/Implementation = 'UserRole'), not names this endpoint
    // chooses: a save under any other pair produces a row nothing composes on the next read.
    private const string UserRoleDomain = "UserRole";
    private const string UserRoleImplementation = "UserRole";

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
            var roleResult = await _roleProvider.Get(req.RoleName, ct).ConfigureAwait(false);
            if (!roleResult.IsSuccess)
            {
                AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, req.RoleName,
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

            var granted = await Grant(userId, role.Id, role.Name, tenantId, ct).ConfigureAwait(false);
            if (granted != StatusCodes.Status200OK)
            {
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, granted, ct).ConfigureAwait(false);
                return;
            }

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

            AuthorizationEndpointLog.UserRoleAssigned(EndpointLogger, req.RoleName, userIdString);

            await Send.OkAsync(new UserRolesResponse
            {
                UserId = userId,
                Roles = roles!
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthorizationEndpointLog.OperationFailed(EndpointLogger, ex, "assign role", userIdString);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
        }
    }

    // Grants the role in the tenant unless the user already holds it there, and answers with the
    // status the request should fail with -- 200 when the role is held afterwards.
    private async Task<int> Grant(Guid userId, Guid roleId, string roleName, Guid tenantId, CancellationToken ct)
    {
        var userIdString = userId.ToString();
        var heldResult = await _userRoleProvider
            .Find<UserRoleImplementationConfiguration>(
                ur => Guid.TryParse(ur.UserId, out var assigned) && assigned == userId
                    && ur.RoleId == roleId && ur.TenantId == tenantId, ct)
            .ConfigureAwait(false);
        if (!heldResult.IsSuccess || heldResult.Value is null)
        {
            AuthorizationEndpointLog.AuthorizationReadFailed(EndpointLogger, userIdString,
                heldResult.CurrentMessage);
            return StatusCodes.Status500InternalServerError;
        }

        // Assigning a role the user already holds in this tenant changes nothing. Saving again would
        // write a second domain row before the unique (UserId, RoleId, TenantId) index refused the
        // implementation row, leaving half an assignment behind.
        if (heldResult.Value.Count > 0)
            return StatusCodes.Status200OK;

        var config = new UserRoleImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userIdString,
            RoleId = roleId,
            TenantId = tenantId,
            // The tenant is part of the name because the same role can be held in several tenants,
            // and a save by an existing name versions THAT row -- a grant in a second tenant would
            // otherwise replace the grant in the first.
            Name = $"{userId}:{roleName}:{tenantId}",
            AssignedAt = DateTimeOffset.UtcNow
        };

        // Not atomic. A save is two writes -- the domain row then the implementation row -- and
        // nothing rolls the first back if the second fails. Deferred deliberately: this endpoint
        // inherits atomicity the moment the provider's Save gets it, with no change here.
        var assignResult = await _userRoleProvider
            .Save(config, UserRoleDomain, UserRoleImplementation, config.Name, ct).ConfigureAwait(false);
        if (!assignResult.IsSuccess)
        {
            AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, userIdString,
                assignResult.CurrentMessage);
            return StatusCodes.Status400BadRequest;
        }

        return StatusCodes.Status200OK;
    }
}
