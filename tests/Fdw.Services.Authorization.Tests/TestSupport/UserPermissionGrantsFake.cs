using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Reads the grants of one person from a catalog and assignments a test holds, the way the <c>UserPermissionGrant</c> view does: an assignment
/// that names the person (whichever way the text spells the id) whose role exists is one row for each permission the role's own tenant wrote for
/// it, or one row with no permission when it has none. An assignment with no tenant never matches a tenant, as a row with a tenant that is not
/// the caller's does not.
/// </summary>
internal sealed class UserPermissionGrantsFake : IUserPermissionGrants
{
    private readonly IReadOnlyList<IRoleImplementationConfiguration> _roles;
    private readonly IReadOnlyList<IPermissionImplementationConfiguration> _permissions;
    private readonly IReadOnlyList<IRolePermissionImplementationConfiguration> _grants;
    private readonly IReadOnlyList<IUserRoleImplementationConfiguration> _assignments;
    private readonly string? _unreadable;

    private UserPermissionGrantsFake(
        IReadOnlyList<IRoleImplementationConfiguration> roles,
        IReadOnlyList<IPermissionImplementationConfiguration> permissions,
        IReadOnlyList<IRolePermissionImplementationConfiguration> grants,
        IReadOnlyList<IUserRoleImplementationConfiguration> assignments,
        string? unreadable)
    {
        _roles = roles;
        _permissions = permissions;
        _grants = grants;
        _assignments = assignments;
        _unreadable = unreadable;
    }

    /// <summary>Holds this catalog and these assignments.</summary>
    public static UserPermissionGrantsFake Of(
        IEnumerable<IRoleImplementationConfiguration> roles,
        IEnumerable<IPermissionImplementationConfiguration> permissions,
        IEnumerable<IRolePermissionImplementationConfiguration> grants,
        IEnumerable<IUserRoleImplementationConfiguration> assignments)
        => new([.. roles], [.. permissions], [.. grants], [.. assignments], null);

    /// <summary>Fails every read with this reason.</summary>
    public static UserPermissionGrantsFake Unreadable(string reason) => new([], [], [], [], reason);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<UserPermissionGrant>>> Get(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_unreadable is null
            ? GenericResult<IReadOnlyList<UserPermissionGrant>>.Success(Rows(userId))
            : GenericResult<IReadOnlyList<UserPermissionGrant>>.Failure(new GenericMessage(_unreadable)));

    private List<UserPermissionGrant> Rows(Guid userId)
    {
        var rows = new List<UserPermissionGrant>();
        foreach (var assignment in _assignments.Where(assignment => Guid.TryParse(assignment.UserId, out var holder) && holder == userId))
        {
            if (_roles.FirstOrDefault(role => role.Id == assignment.RoleId) is not { } role)
            {
                continue;
            }

            var owner = role.TenantId ?? Guid.Empty;
            var written = role.TenantId is { } tenant
                ? _grants.Where(grant => grant.RoleId == role.Id && grant.TenantId == tenant).ToList()
                : [];
            var names = written
                .Select(grant => _permissions.FirstOrDefault(permission => permission.Id == grant.PermissionId)?.Name)
                .ToList();
            if (names.Count == 0)
            {
                names.Add(null);
            }

            rows.AddRange(names.Select(name => new UserPermissionGrant(
                assignment.TenantId ?? Guid.Empty, assignment.ExpiresAt, role.Id, role.Name, role.IsTenantScoped, owner, name)));
        }

        return rows;
    }
}
