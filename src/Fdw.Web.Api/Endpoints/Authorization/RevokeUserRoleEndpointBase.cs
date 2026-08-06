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
    // Why: RoleConfigurationProvider replaces IOptionsMonitor<List<RoleConfiguration>> for role lookups.
    private readonly RoleConfigurationProvider _roleProvider;
    // Why: UserRoleConfigurationProvider replaces IOptionsMonitor<List<UserRoleConfiguration>>
    // for dual-source (ctrl + cfg) user-role queries.
    private readonly UserRoleConfigurationProvider _userRoleProvider;

    // Why: route binds {Name} as string so we look up the user by name and resolve to a Guid here.
    // Why: UserConfigurationProvider replaces the deleted IUserService wrapper.
    private readonly UserConfigurationProvider _userProvider;
    // Why: IConfigurationGateway is the single connection used by the authorization domain; opening
    // a transaction on it ensures the role deletion is atomic.
    private readonly IConfigurationGateway _configurationGateway;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected RevokeUserRoleEndpointBase(
        RoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider,
        UserConfigurationProvider userProvider,
        IConfigurationGateway configurationGateway)
    {
        _roleProvider = roleProvider;
        _userRoleProvider = userRoleProvider;
        _userProvider = userProvider;
        _configurationGateway = configurationGateway;
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
    /// Gets the RBAC policy required by this endpoint. Defaults to "users:write".
    /// </summary>
    // Why: the standard CRUD tier for this resource. This endpoint previously required ":delete"
    // as an ad-hoc "Admin-only" tier, because the seeded Operator role is granted ":write" on
    // every resource by a blanket rule and would otherwise have inherited user administration.
    // The grant was the wrong thing to work around: user/role admin is now carved out of
    // Operator in the seed, so these permissions can mean exactly what they say (FDW-634).
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        // Why: {IdOrName} accepts a Guid id or a username, matching the assign route. Binding it as
        // a string avoids the Guid binder rejecting "/users/admin/roles/Admin" with a parse error.
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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var userResult = await _userProvider.ResolveUser(req.IdOrName, ct).ConfigureAwait(false);
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
            var role = await _roleProvider.GetRole(req.RoleName, ct).ConfigureAwait(false);
            if (role is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var userRolesResult = await _userRoleProvider.GetByUser(userIdString, ct).ConfigureAwait(false);
            // Why: FDW-532 — GetByUser now returns IGenericResult; fail-closed on provider failure.
            if (!userRolesResult.IsSuccess || userRolesResult.Value is null)
            {
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
                return;
            }

            var existing = userRolesResult.Value.FirstOrDefault(ur => ur.RoleId == role.Id);

            // Why: idempotent delete — if the role isn't currently assigned, treat as already-revoked
            // and return 204. The DELETE verb's HTTP semantic is "ensure this is gone", not "fail if
            // it never existed".
            if (existing is null)
            {
                await Send.NoContentAsync(ct).ConfigureAwait(false);
                return;
            }

            var revokeResult = await RevokeRoleAtomically(existing, userId, userIdString, ct).ConfigureAwait(false);
            if (!revokeResult.IsSuccess)
                return;

            AuthorizationEndpointLog.UserRoleRevoked(EndpointLogger, req.RoleName, userIdString);
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthorizationEndpointLog.OperationFailed(EndpointLogger, ex, "revoke role", userIdString);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
        }
    }

    // Why: Extracted to keep HandleAsync below the FDW007 cyclomatic-complexity threshold.
    // Returns success when the transaction commits; on failure, the HTTP response is already sent.
    private async Task<IGenericResult> RevokeRoleAtomically(
        UserRoleConfiguration existing, Guid userId, string userIdString, CancellationToken ct)
    {
        var txnResult = await _configurationGateway.BeginTransaction(
            _userRoleProvider.DataStoreName, ct).ConfigureAwait(false);
        if (!txnResult.IsSuccess || txnResult.Value == null)
        {
            var reason = txnResult.CurrentMessage ?? "Transaction could not be opened";
            var msg = AuthorizationEndpointLog.TransactionOpenFailed(EndpointLogger, userIdString, reason);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return GenericResult.Failure(msg);
        }

        var txn = txnResult.Value;
        try
        {
            var deleteResult = await _userRoleProvider.DeleteInTransaction(existing.Id, txn, ct).ConfigureAwait(false);
            if (!deleteResult.IsSuccess)
            {
                var rollbackResult = await txn.Rollback(ct).ConfigureAwait(false);
                if (!rollbackResult.IsSuccess)
                    AuthorizationEndpointLog.RollbackFailed(EndpointLogger, userIdString, rollbackResult.CurrentMessage);
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, userIdString,
                    deleteResult.CurrentMessage ?? "Role delete failed");
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
                return deleteResult;
            }

            var commitResult = await txn.Commit(ct).ConfigureAwait(false);
            if (!commitResult.IsSuccess)
            {
                AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, userIdString,
                    commitResult.CurrentMessage ?? "Commit failed");
                await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
                return commitResult;
            }

            return GenericResult.Success();
        }
        finally
        {
            await txn.DisposeAsync().ConfigureAwait(false);
        }
    }
}
