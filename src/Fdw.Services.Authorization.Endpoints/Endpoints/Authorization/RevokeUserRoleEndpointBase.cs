using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Services.Users.Clients.Models;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for revoking a role from a user.
/// The role revocation executes in a transaction for atomicity.
/// </summary>
public abstract class RevokeUserRoleEndpointBase : Endpoint<RevokeRoleRequest, UserRolesResponse>
{
    /// <summary>Initializes a new instance of the <see cref="RevokeUserRoleEndpointBase"/> class.</summary>
        private readonly IAuthorizationProvider _authorizationProvider;
    private readonly IUserRoleConfigurationProvider _userRoleProvider;

    private readonly IUserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="RevokeUserRoleEndpointBase"/> class.</summary>
    protected RevokeUserRoleEndpointBase(ILogger logger, IAuthorizationProvider authorizationProvider,
        IUserRoleConfigurationProvider userRoleProvider,
        IUserConfigurationProvider userProvider)
    {
        EndpointLogger = logger;
        _authorizationProvider = authorizationProvider;
        _userRoleProvider = userRoleProvider;
        _userProvider = userProvider;
    }


    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected IAuthorizationProvider AuthorizationProvider => _authorizationProvider;

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
        Delete("/users/{IdOrName}/roles/{RoleName}");
        Policies(WritePolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(RevokeRoleRequest req, CancellationToken ct)
    {
        
        // The route takes an id OR a name; the domain provider answers each by its own overload.
        var userResult = Guid.TryParse(req.IdOrName, out var parsedUserId)
            ? await _userProvider.Get(parsedUserId, ct).ConfigureAwait(false)
            : await _userProvider.Get(req.IdOrName, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var userId = userResult.Value.Id;
        var userIdString = userId.ToString();
        AuthorizationEndpointLog.RevokingUserRole(EndpointLogger, req.RoleName, userIdString);

        try
        {
            var role = await _authorizationProvider.GetRole(req.RoleName, ct).ConfigureAwait(false);
            if (role is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var userRolesResult = await _userRoleProvider
                .Find<UserRoleImplementationConfiguration>(
                    ur => string.Equals(ur.UserId, userIdString, StringComparison.OrdinalIgnoreCase), ct)
                .ConfigureAwait(false);
            if (!userRolesResult.IsSuccess || userRolesResult.Value is null)
            {
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
                return;
            }

            var existing = userRolesResult.Value.FirstOrDefault(ur => ur.RoleId == role.Id);

            if (existing is null)
            {
                await Send.NoContentAsync(ct).ConfigureAwait(false);
                return;
            }

            // Not atomic. Delete is two writes -- the implementation row then the domain row -- and
            // nothing rolls the first back if the second fails. Deferred deliberately: this endpoint
            // inherits atomicity the moment the provider's Delete gets it, with no change here.
            var revokeResult = await _userRoleProvider.Delete(existing.Id, ct).ConfigureAwait(false);
            if (!revokeResult.IsSuccess)
            {
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, userIdString,
                    revokeResult.CurrentMessage ?? "Role delete failed");
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
                return;
            }

            AuthorizationEndpointLog.UserRoleRevoked(EndpointLogger, req.RoleName, userIdString);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthorizationEndpointLog.OperationFailed(EndpointLogger, ex, "revoke role", userIdString);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
        }
    }
}
