using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Gives a role to a person in a tenant by writing the grant (FDW-1578).
/// </summary>
/// <remarks>
/// The domain and implementation are values the seed already writes (<c>authz.UserRole</c> Domain and Implementation are both
/// <c>UserRole</c>), not names chosen here: a save under any other pair produces a row nothing composes on the next read. The tenant is
/// part of the grant's name because the same role can be held in several tenants, and a save by an existing name versions THAT row, so a
/// grant in a second tenant would otherwise replace the grant in the first. A save is two writes, the domain row then the implementation
/// row, and nothing rolls the first back if the second fails: this inherits atomicity the moment the provider's save gets it.
/// </remarks>
public sealed class UserRoleAssignment : IUserRoleAssignment
{
    private const string UserRoleDomain = "UserRole";
    private const string UserRoleImplementation = "UserRole";

    private readonly IRoleConfigurationProvider _roles;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly ILogger<UserRoleAssignment> _logger;

    /// <summary>Initializes a new instance of the <see cref="UserRoleAssignment"/> class.</summary>
    /// <param name="roles">Reads the role catalogue.</param>
    /// <param name="userRoles">Reads and writes the grants.</param>
    /// <param name="logger">The logger.</param>
    public UserRoleAssignment(IRoleConfigurationProvider roles, IUserRoleConfigurationProvider userRoles, ILogger<UserRoleAssignment>? logger)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _userRoles = userRoles ?? throw new ArgumentNullException(nameof(userRoles));
        _logger = logger ?? NullLogger<UserRoleAssignment>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Assign(Guid userId, string roleName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        var found = await _roles.Get(roleName, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
        {
            return GenericResult.Failure(UserRoleAssignmentLog.RoleNotRead(_logger, roleName, found.CurrentMessage));
        }

        if (found.Value is null)
        {
            return GenericResult.Failure(UserRoleAssignmentLog.RoleNotFound(_logger, roleName));
        }

        var roleId = found.Value.Id;

        var held = await _userRoles
            .Find<UserRoleImplementationConfiguration>(
                grant => Guid.TryParse(grant.UserId, out var assigned) && assigned == userId
                    && grant.RoleId == roleId && grant.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!held.IsSuccess || held.Value is null)
        {
            return GenericResult.Failure(UserRoleAssignmentLog.HeldRolesNotRead(_logger, userId, tenantId, held.CurrentMessage));
        }

        // Why nothing is written: saving again would write a second domain row before the unique (UserId, RoleId, TenantId) index refused
        // the implementation row, leaving half an assignment behind.
        if (held.Value.Count > 0)
        {
            UserRoleAssignmentLog.AlreadyHeld(_logger, found.Value.Name, userId, tenantId);
            return GenericResult.Success();
        }

        var name = $"{userId}:{found.Value.Name}:{tenantId}";
        var saved = await _userRoles
            .Save(
                new UserRoleImplementationConfiguration
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.ToString(),
                    RoleId = roleId,
                    TenantId = tenantId,
                    Name = name,
                    AssignedAt = DateTimeOffset.UtcNow,
                },
                UserRoleDomain, UserRoleImplementation, name, cancellationToken)
            .ConfigureAwait(false);
        if (!saved.IsSuccess)
        {
            return GenericResult.Failure(UserRoleAssignmentLog.NotSaved(_logger, found.Value.Name, userId, tenantId, saved.CurrentMessage));
        }

        UserRoleAssignmentLog.Assigned(_logger, found.Value.Name, userId, tenantId);
        return GenericResult.Success();
    }
}
