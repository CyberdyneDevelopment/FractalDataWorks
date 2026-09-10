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
    /// <summary>Initializes a new instance of the <see cref="AssignUserRoleEndpointBase"/> class.</summary>
        private readonly IAuthorizationProvider _authorizationProvider;
    private readonly UserRoleConfigurationProvider _userRoleProvider;
    private readonly IUserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="AssignUserRoleEndpointBase"/> class.</summary>
    protected AssignUserRoleEndpointBase(ILogger logger, IAuthorizationProvider authorizationProvider,
        UserRoleConfigurationProvider userRoleProvider,
        IUserConfigurationProvider userProvider)
    {
        EndpointLogger = logger;
        _authorizationProvider = authorizationProvider;
        _userRoleProvider = userRoleProvider;
        _userProvider = userProvider;
    }


    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected IUserConfigurationProvider UserProvider => _userProvider;

    /// <summary>
    /// Gets the role configuration provider.
    /// </summary>
    protected IAuthorizationProvider AuthorizationProvider => _authorizationProvider;

    /// <summary>
    /// Gets the user-role configuration provider.
    /// </summary>
    protected UserRoleConfigurationProvider UserRoleProvider => _userRoleProvider;

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
            var role = await _authorizationProvider.GetRole(req.RoleName, ct).ConfigureAwait(false);
            if (role is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

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

            var assignResult = await AssignRoleAtomically(userId, config, userIdString, ct).ConfigureAwait(false);
            if (!assignResult.IsSuccess)
                return;

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

    private async Task<Fdw.Results.IGenericResult> AssignRoleAtomically(
        Guid userId, UserRoleImplementationConfiguration config, string userIdString, CancellationToken ct)
    {
        var txnResult = await _userRoleProvider.BeginTransaction(ct).ConfigureAwait(false);
        if (!txnResult.IsSuccess || txnResult.Value == null)
        {
            var reason = txnResult.CurrentMessage ?? "Transaction could not be opened";
            var msg = AuthorizationEndpointLog.TransactionOpenFailed(EndpointLogger, userIdString, reason);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return Fdw.Results.GenericResult.Failure(msg);
        }

        var txn = txnResult.Value;
        try
        {
            var saveResult = await _userRoleProvider.SaveInTransaction(config, txn, ct).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
            {
                var rollbackResult = await txn.Rollback(ct).ConfigureAwait(false);
                if (!rollbackResult.IsSuccess)
                    AuthorizationEndpointLog.RollbackFailed(EndpointLogger, userIdString, rollbackResult.CurrentMessage);
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, userIdString,
                    saveResult.CurrentMessage ?? "Role save failed");
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 400, ct).ConfigureAwait(false);
                return saveResult;
            }

            var commitResult = await txn.Commit(ct).ConfigureAwait(false);
            if (!commitResult.IsSuccess)
            {
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, userIdString,
                    commitResult.CurrentMessage ?? "Commit failed");
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
                return commitResult;
            }


            // The write used DeleteInTransaction/SaveInTransaction, which defer to this
            // transaction and so CANNOT invalidate before commit -- the provider says as much on
            // InvalidateCache. Without this call the row is correct in storage and the running
            // host keeps serving the cached list until it restarts, which for a revoke means the
            // permission stays live after an administrator was told it was gone. Worse than the
            // read filter it sits behind (FDW-732), because the database looks right to anyone
            // who checks. See FDW-736.
            _userRoleProvider.InvalidateCache();
            return Fdw.Results.GenericResult.Success();
        }
        finally
        {
            await txn.DisposeAsync().ConfigureAwait(false);
        }
    }
}
