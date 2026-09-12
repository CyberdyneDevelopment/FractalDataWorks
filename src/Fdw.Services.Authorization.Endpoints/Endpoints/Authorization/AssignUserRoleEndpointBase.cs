using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
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

            var config = new UserRoleImplementationConfiguration
            {
                Id = Guid.NewGuid(),
                UserId = userIdString,
                RoleId = role.Id,
                TenantId = req.TenantId,
                // The seed writes this name as {userId}:{roleName} (CONCAT(ur.UserId, ':', r.Name));
                // minting it from ids instead produces a row the seed's own idempotence check misses.
                Name = $"{userId}:{role.Name}",
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
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 400, ct).ConfigureAwait(false);
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
                    // authz.UserRoleImplementation.UserId is a Guid stored as VARCHAR, so comparing
                    // the TEXT makes the match depend on how each side happened to spell it. Both
                    // sides are parsed and compared as the Guids they are (FDW-532).
                    ur => Guid.TryParse(ur.UserId, out var assigned) && assigned == userId, ct)
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
}
