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
    // Why: RoleConfigurationProvider replaces IOptionsMonitor<List<RoleConfiguration>> for role lookups.
    private readonly RoleConfigurationProvider _roleProvider;
    // Why: UserRoleConfigurationProvider replaces IOptionsMonitor<List<UserRoleConfiguration>>
    // for dual-source (ctrl + cfg) user-role queries.
    private readonly UserRoleConfigurationProvider _userRoleProvider;
    // Why: IConfigurationGateway is the single connection used by the authorization domain; opening
    // a transaction on it ensures the role write is atomic.
    private readonly IConfigurationGateway _configurationGateway;
    // Why: owns the shared id-or-name resolution every user-scoped route goes through.
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected AssignUserRoleEndpointBase(
        RoleConfigurationProvider roleProvider,
        UserRoleConfigurationProvider userRoleProvider,
        IConfigurationGateway configurationGateway,
        UserConfigurationProvider userProvider)
    {
        _roleProvider = roleProvider;
        _userRoleProvider = userRoleProvider;
        _configurationGateway = configurationGateway;
        _userProvider = userProvider;
    }

    /// <summary>
    /// Gets the user provider.
    /// </summary>
    protected UserConfigurationProvider UserProvider => _userProvider;

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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        // Why: resolve through the shared id-or-name resolver so this route accepts exactly what the
        // revoke and get-roles routes accept. Assign previously bound a raw Guid while those two
        // resolved a username, so a caller could add a role it was then unable to remove.
        var lookup = await _userProvider.ResolveUser(req.IdOrName, ct).ConfigureAwait(false);
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
            var role = await _roleProvider.GetRole(req.RoleName, ct).ConfigureAwait(false);
            if (role is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var config = new UserRoleConfiguration
            {
                Id = Guid.NewGuid(),
                UserId = userIdString,
                RoleId = role.Id,
                TenantId = req.TenantId,
                Name = $"{userId}:{role.Id}",
                AssignedAt = DateTimeOffset.UtcNow
            };

            var assignResult = await AssignRoleAtomically(userId, config, userIdString, ct).ConfigureAwait(false);
            if (!assignResult.IsSuccess)
                return;

            var allRoles = await _roleProvider.GetAllRoles(ct).ConfigureAwait(false);
            var userRolesResult = await _userRoleProvider.GetByUser(userIdString, ct).ConfigureAwait(false);
            // Why: FDW-532 — GetByUser now returns IGenericResult; after a successful assignment
            // we still need to return the updated role list. If the load fails, return 500.
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

    // Why: Extracted to keep HandleAsync below the FDW007 cyclomatic-complexity threshold.
    // Returns success when the transaction commits; on failure, the HTTP response is already sent.
    private async Task<Fdw.Results.IGenericResult> AssignRoleAtomically(
        Guid userId, UserRoleConfiguration config, string userIdString, CancellationToken ct)
    {
        var txnResult = await _configurationGateway.BeginTransaction(
            _userRoleProvider.DataStoreName, ct).ConfigureAwait(false);
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

            return Fdw.Results.GenericResult.Success();
        }
        finally
        {
            await txn.DisposeAsync().ConfigureAwait(false);
        }
    }
}
