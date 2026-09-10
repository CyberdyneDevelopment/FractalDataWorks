using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Fdw.Services.Users.Clients.Models;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for getting roles assigned to a user.
/// </summary>
public abstract class GetUserRolesEndpointBase : Endpoint<GetUserRolesRequest, UserRolesResponse>
{
    /// <summary>Initializes a new instance of the <see cref="GetUserRolesEndpointBase"/> class.</summary>
        private readonly IAuthorizationProvider _authorizationProvider;
    private readonly IUserRoleConfigurationProvider _userRoleProvider;

    private readonly IUserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="GetUserRolesEndpointBase"/> class.</summary>
    protected GetUserRolesEndpointBase(ILogger logger, IAuthorizationProvider authorizationProvider,
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
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:read".
    /// </summary>
    protected virtual string ReadPolicy => "users:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/users/{IdOrName}/roles");
        Policies(ReadPolicy);
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(GetUserRolesRequest req, CancellationToken ct)
    {
        
        try
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
            var allRoles = await _authorizationProvider.GetAllRoles(ct).ConfigureAwait(false);

            var userRolesResult = await _userRoleProvider
                .Find<UserRoleImplementationConfiguration>(
                    ur => string.Equals(ur.UserId, userIdString, StringComparison.OrdinalIgnoreCase), ct)
                .ConfigureAwait(false);
            if (!userRolesResult.IsSuccess || userRolesResult.Value is null)
            {
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
                return;
            }

            var roles = userRolesResult.Value
                .Select(ur => allRoles.FirstOrDefault(r => r.Id == ur.RoleId)?.Name)
                .Where(name => name is not null)
                .ToList()!;

            await Send.OkAsync(new UserRolesResponse
            {
                UserId = userId,
                Roles = roles!
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthorizationEndpointLog.OperationFailed(EndpointLogger, ex, "get user roles", req.IdOrName);
            await Send.ResponseAsync(new UserRolesResponse { UserId = Guid.Empty }, 500, ct).ConfigureAwait(false);
        }
    }
}
