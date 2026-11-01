using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
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
        private readonly IRoleConfigurationProvider _roleProvider;
    private readonly IUserRoleConfigurationProvider _userRoleProvider;

    private readonly IUserConfigurationProvider _userProvider;
    private readonly ITokenRevocationStore _revocations;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="RevokeUserRoleEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="roleProvider">Resolves the role by name.</param>
    /// <param name="userRoleProvider">Reads and deletes the user's role assignments.</param>
    /// <param name="userProvider">Resolves the user by id or name.</param>
    /// <param name="revocations">
    /// Ends the user's live sessions once the role is gone: their tokens carry the role's permissions
    /// until they expire, so removing the assignment alone would not take effect until then.
    /// </param>
    protected RevokeUserRoleEndpointBase(ILogger logger, IRoleConfigurationProvider roleProvider,
        IUserRoleConfigurationProvider userRoleProvider,
        IUserConfigurationProvider userProvider,
        ITokenRevocationStore revocations)
    {
        EndpointLogger = logger;
        _roleProvider = roleProvider;
        _userRoleProvider = userRoleProvider;
        _userProvider = userProvider;
        _revocations = revocations;
    }


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

        // A role is held per tenant (FDW-825); revoking acts in the tenant the caller is acting in, and
        // leaves the same role held in any other tenant untouched.
        if (!Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
        {
            AuthorizationEndpointLog.RoleChangeWithoutTenant(EndpointLogger, userIdString);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        AuthorizationEndpointLog.RevokingUserRole(EndpointLogger, req.RoleName, userIdString);

        try
        {
            await RevokeRole(req.RoleName, userId, tenantId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AuthorizationEndpointLog.OperationFailed(EndpointLogger, ex, "revoke role", userIdString);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
        }
    }

    // Resolves the role, checks the caller may take it away and that the user holds it in the tenant, and that
    // removing it leaves a platform administrator, then removes it.
    private async Task RevokeRole(string roleName, Guid userId, Guid tenantId, CancellationToken ct)
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

        // Why the same rule as giving: taking Admin from an administrator is as strong as giving it, so a caller
        // may take away only a role whose rights they hold themselves (FDW-1577).
        var allowed = await Resolve<IGrantRule>().CanGive([role.Name], tenantId, ct).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(allowed, HttpContext)).ConfigureAwait(false);
            return;
        }

        var userRolesResult = await _userRoleProvider
            .Find<UserRoleImplementationConfiguration>(
                // authz.UserRoleConfiguration.UserId is a Guid stored as VARCHAR, so comparing
                // the TEXT makes the match depend on how each side happened to spell it. Both
                // sides are parsed and compared as the Guids they are (FDW-532).
                ur => Guid.TryParse(ur.UserId, out var assigned) && assigned == userId, ct)
            .ConfigureAwait(false);
        if (!userRolesResult.IsSuccess || userRolesResult.Value is null)
        {
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }

        var existing = userRolesResult.Value.FirstOrDefault(ur => ur.RoleId == role.Id && ur.TenantId == tenantId);

        if (existing is null)
        {
            await Send.NoContentAsync(ct).ConfigureAwait(false);
            return;
        }

        var kept = await Resolve<IPlatformAdministratorGuard>().CanRemove(userId, role.Name, tenantId, ct).ConfigureAwait(false);
        if (kept.IsFailure)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(kept, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Remove(existing.Id, roleName, role.Name, userId, ct).ConfigureAwait(false);
    }

    // Deletes the assignment, ends the user's live sessions, and records the revocation in the audit trail.
    private async Task Remove(Guid assignmentId, string requestedRoleName, string roleName, Guid userId, CancellationToken ct)
    {
        var userIdString = userId.ToString();

        // Not atomic. Delete is two writes -- the implementation row then the domain row -- and
        // nothing rolls the first back if the second fails. Deferred deliberately: this endpoint
        // inherits atomicity the moment the provider's Delete gets it, with no change here.
        var revokeResult = await _userRoleProvider.Delete(assignmentId, ct).ConfigureAwait(false);
        if (!revokeResult.IsSuccess)
        {
            AuthorizationEndpointLog.AtomicRoleChangeFailed(EndpointLogger, userIdString,
                revokeResult.CurrentMessage);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }

        // Why after the delete and not before: every token issued up to this moment is cut off, and
        // any sign-in from here on reads the assignments without the role, so no token can be
        // issued in between that still carries it.
        var ended = await _revocations.RevokeAll(userId, DateTimeOffset.UtcNow, ct).ConfigureAwait(false);
        if (ended.IsFailure)
        {
            AuthorizationEndpointLog.RoleRevokedSessionsNotEnded(EndpointLogger, requestedRoleName, userIdString);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }

        var audited = await RoleChangeAudit.RecordUserRole(Resolve<IAdministrativeAudit>(), User, "RoleRevoked", userId,
            roleName, roleName, null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.RoleChangeNotAudited(EndpointLogger, "revocation", roleName, userIdString,
                audited.CurrentMessage);
            await Send.ResponseAsync(new UserRolesResponse { UserId = userId }, 500, ct).ConfigureAwait(false);
            return;
        }

        AuthorizationEndpointLog.UserRoleRevoked(EndpointLogger, requestedRoleName, userIdString);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
