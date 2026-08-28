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
    private readonly RoleConfigurationProvider _roleProvider;
    private readonly UserRoleConfigurationProvider _userRoleProvider;

    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected GetUserRolesEndpointBase(
        RoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider,
        UserConfigurationProvider userProvider)
    {
        _roleProvider = roleProvider;
        _userRoleProvider = userRoleProvider;
        _userProvider = userProvider;
    }

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected RoleConfigurationProvider RoleProvider => _roleProvider;

    /// <summary>
    /// Gets the user-role configuration provider.
    /// </summary>
    protected UserRoleConfigurationProvider UserRoleProvider => _userRoleProvider;

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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            var userResult = await _userProvider.ResolveUser(req.IdOrName, ct).ConfigureAwait(false);
            if (!userResult.IsSuccess || userResult.Value is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var userId = userResult.Value.Id;
            var userIdString = userId.ToString();
            var allRoles = await _roleProvider.GetAllRoles(ct).ConfigureAwait(false);

            var userRolesResult = await _userRoleProvider.GetByUser(userIdString, ct).ConfigureAwait(false);
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
