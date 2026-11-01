using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// The roles an organization defines for its own members, kept as managed configuration and mirrored into the access rows of the people
/// who hold them (FDW-1593).
/// </summary>
/// <remarks>
/// A role is saved under the name <c>{OrganizationId}:{RoleName}</c>, so the same role name can exist in two organizations. There is no
/// transaction across the saves and the access rows of the holders, so a change refreshes the holders after it is saved and reports a
/// holder it could not refresh (logged as critical): that person's access is out of date until the change is repeated.
/// </remarks>
public sealed class OrganizationRoles : IOrganizationRoles
{
    private const string RoleDomain = "OrganizationRole";

    private readonly IOrganizationScope _scope;
    private readonly IOrganizationRoleConfigurationProvider _roles;
    private readonly TenantOrgAccessConfigurationProvider _access;
    private readonly IGrantRule _grantRule;
    private readonly ILogger<OrganizationRoles> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationRoles"/> class.</summary>
    /// <param name="scope">Judges whether the caller may administer the organization.</param>
    /// <param name="roles">Reads and writes the role definitions.</param>
    /// <param name="access">Reads and writes the access rows of the people who hold the roles.</param>
    /// <param name="grantRule">Judges whether the caller holds what a role carries.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationRoles(
        IOrganizationScope scope,
        IOrganizationRoleConfigurationProvider roles,
        TenantOrgAccessConfigurationProvider access,
        IGrantRule grantRule,
        ILogger<OrganizationRoles>? logger)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _grantRule = grantRule ?? throw new ArgumentNullException(nameof(grantRule));
        _logger = logger ?? NullLogger<OrganizationRoles>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OrganizationRoleView>> Define(
        Guid organizationId, string roleName, string? description, IReadOnlyList<string> permissions, CancellationToken cancellationToken = default)
    {
        var judged = await Judge(organizationId, roleName, permissions, cancellationToken).ConfigureAwait(false);
        if (judged.IsFailure || judged.Value is null)
        {
            return judged.ToNewResult<OrganizationRoleView>();
        }

        var existing = await Find(organizationId, roleName, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return existing.ToNewResult<OrganizationRoleView>();
        }

        if (existing.Value is not null)
        {
            return GenericResult<OrganizationRoleView>.Failure(OrganizationAccessLog.RoleExists(_logger, organizationId, roleName));
        }

        var saved = await Save(organizationId, roleName, description, false, judged.Value.Permissions, null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<OrganizationRoleView>();
        }

        OrganizationAccessLog.RoleDefined(_logger, roleName, organizationId, judged.Value.Permissions.Count);
        return GenericResult<OrganizationRoleView>.Success(new OrganizationRoleView(roleName, description, false, judged.Value.Permissions));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OrganizationRoleView>> Redefine(
        Guid organizationId, string roleName, string? description, IReadOnlyList<string> permissions, CancellationToken cancellationToken = default)
    {
        var judged = await Judge(organizationId, roleName, permissions, cancellationToken).ConfigureAwait(false);
        if (judged.IsFailure || judged.Value is null)
        {
            return judged.ToNewResult<OrganizationRoleView>();
        }

        var existing = await Find(organizationId, roleName, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure || existing.Value is null)
        {
            return existing.IsFailure
                ? existing.ToNewResult<OrganizationRoleView>()
                : GenericResult<OrganizationRoleView>.Failure(OrganizationAccessLog.RoleNotFound(_logger, organizationId, roleName));
        }

        var saved = await Save(organizationId, roleName, description, existing.Value.IsBuiltIn, judged.Value.Permissions, existing.Value, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<OrganizationRoleView>();
        }

        var refreshed = await Refresh(organizationId, judged.Value.TenantId, roleName, judged.Value.Permissions, cancellationToken).ConfigureAwait(false);
        return refreshed.IsFailure
            ? refreshed.ToNewResult<OrganizationRoleView>()
            : GenericResult<OrganizationRoleView>.Success(new OrganizationRoleView(roleName, description, existing.Value.IsBuiltIn, judged.Value.Permissions));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(Guid organizationId, string roleName, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(OrganizationRights.Write, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant;
        }

        var existing = await Find(organizationId, roleName, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure || existing.Value is null)
        {
            return existing.IsFailure ? existing : GenericResult.Failure(OrganizationAccessLog.RoleNotFound(_logger, organizationId, roleName));
        }

        if (existing.Value.IsBuiltIn)
        {
            return GenericResult.Failure(OrganizationAccessLog.RoleBuiltIn(_logger, roleName, organizationId));
        }

        var holders = await Holders(organizationId, tenant.Value, roleName, cancellationToken).ConfigureAwait(false);
        if (holders.IsFailure || holders.Value is null)
        {
            return holders;
        }

        if (holders.Value.Count > 0)
        {
            return GenericResult.Failure(OrganizationAccessLog.RoleHeld(_logger, roleName, organizationId, holders.Value.Count));
        }

        var removed = await _roles.Delete(NameOf(organizationId, roleName), cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"Role '{roleName}' of organization '{organizationId}'", removed.CurrentMessage));
        }

        OrganizationAccessLog.RoleDeleted(_logger, roleName, organizationId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OrganizationRoleView>> Get(Guid organizationId, string roleName, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(OrganizationRights.Read, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<OrganizationRoleView>();
        }

        var existing = await Find(organizationId, roleName, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure || existing.Value is null)
        {
            return existing.IsFailure
                ? existing.ToNewResult<OrganizationRoleView>()
                : GenericResult<OrganizationRoleView>.Failure(OrganizationAccessLog.RoleNotFound(_logger, organizationId, roleName));
        }

        return GenericResult<OrganizationRoleView>.Success(ViewOf(existing.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<OrganizationRoleView>>> List(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(OrganizationRights.Read, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<IReadOnlyList<OrganizationRoleView>>();
        }

        var all = await _roles.Get(ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
        {
            return GenericResult<IReadOnlyList<OrganizationRoleView>>.Failure(
                OrganizationAccessLog.ReadFailed(_logger, $"The roles of organization '{organizationId}'", all.CurrentMessage));
        }

        return GenericResult<IReadOnlyList<OrganizationRoleView>>.Success(
            [.. all.Value.Where(role => Live(role, organizationId)).OrderBy(role => role.RoleName, StringComparer.OrdinalIgnoreCase).Select(ViewOf)]);
    }

    // The checks every define and redefine makes first, in the order that discloses least: the organization the caller may administer,
    // a name, at least one permission, and the caller holding every permission the role carries.
    private async Task<IGenericResult<Judged>> Judge(Guid organizationId, string roleName, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var tenant = await _scope.TenantOf(OrganizationRights.Write, organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<Judged>();
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            return GenericResult<Judged>.Failure(OrganizationAccessLog.RoleNameRequired(_logger, organizationId));
        }

        var distinct = permissions.Where(permission => !string.IsNullOrWhiteSpace(permission)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (distinct.Count == 0)
        {
            return GenericResult<Judged>.Failure(OrganizationAccessLog.RoleNeedsPermission(_logger, roleName, organizationId));
        }

        var allowed = _grantRule.CanDefine(distinct, tenant.Value);
        return allowed.IsFailure
            ? allowed.ToNewResult<Judged>()
            : GenericResult<Judged>.Success(new Judged(tenant.Value, distinct));
    }

    private sealed record Judged(Guid TenantId, IReadOnlyList<string> Permissions);

    private static string NameOf(Guid organizationId, string roleName) => $"{organizationId}:{roleName}";

    private static bool Live(IOrganizationRoleImplementationConfiguration role, Guid organizationId)
        => role.OrganizationId == organizationId && role.IsCurrent && !role.IsDeleted;

    private static OrganizationRoleView ViewOf(IOrganizationRoleImplementationConfiguration role)
        => new(role.RoleName, role.Description, role.IsBuiltIn, [.. role.Permissions.Select(permission => permission.PermissionName)]);

    // The live role of an organization with this name, composed with its permissions; Success with a null value when there is none.
    private async Task<IGenericResult<IOrganizationRoleImplementationConfiguration?>> Find(Guid organizationId, string roleName, CancellationToken cancellationToken)
    {
        var all = await _roles.Get(ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
        {
            return GenericResult<IOrganizationRoleImplementationConfiguration?>.Failure(
                OrganizationAccessLog.ReadFailed(_logger, $"The roles of organization '{organizationId}'", all.CurrentMessage));
        }

        return GenericResult<IOrganizationRoleImplementationConfiguration?>.Success(
            all.Value.FirstOrDefault(role => Live(role, organizationId) && string.Equals(role.RoleName, roleName, StringComparison.OrdinalIgnoreCase)));
    }

    // Writes the role: a new one when none is held, otherwise the held role updated under its own Id, so a role removed since it was read
    // is not brought back by the write.
    private async Task<IGenericResult> Save(
        Guid organizationId, string roleName, string? description, bool isBuiltIn, IReadOnlyList<string> permissions,
        IOrganizationRoleImplementationConfiguration? held, CancellationToken cancellationToken)
    {
        var name = NameOf(organizationId, roleName);
        var role = new OrganizationRoleImplementationConfiguration
        {
            Name = name,
            OrganizationId = organizationId,
            RoleName = roleName,
            Description = description,
            IsBuiltIn = isBuiltIn,
            Permissions = [.. permissions.Select(permission => new OrganizationRolePermissionConfiguration { PermissionName = permission })],
        };
        if (held is not null)
        {
            role.Id = held.Id;
        }

        var saved = held is null
            ? await _roles.Save(role, RoleDomain, RoleDomain, name, cancellationToken).ConfigureAwait(false)
            : await _roles.Update(role, RoleDomain, RoleDomain, name, cancellationToken).ConfigureAwait(false);
        return saved.IsSuccess
            ? saved
            : GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"Role '{roleName}' of organization '{organizationId}'", saved.CurrentMessage));
    }

    // The people who hold a role: those with an access row naming it.
    private async Task<IGenericResult<IReadOnlyList<Guid>>> Holders(Guid organizationId, Guid tenantId, string roleName, CancellationToken cancellationToken)
    {
        var rows = await _access.Members(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } read
            ? GenericResult<IReadOnlyList<Guid>>.Success(
                [.. read.Where(row => string.Equals(row.RoleName, roleName, StringComparison.OrdinalIgnoreCase)).Select(row => row.UserId).Distinct()])
            : rows.ToNewResult<IReadOnlyList<Guid>>();
    }

    // After a role changed: every holder gets the permissions it carries now.
    private async Task<IGenericResult> Refresh(
        Guid organizationId, Guid tenantId, string roleName, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var holders = await Holders(organizationId, tenantId, roleName, cancellationToken).ConfigureAwait(false);
        if (holders.IsFailure || holders.Value is null)
        {
            return holders;
        }

        foreach (var userId in holders.Value)
        {
            var replaced = await _access.Replace(userId, tenantId, organizationId, roleName, permissions, cancellationToken).ConfigureAwait(false);
            if (replaced.IsFailure)
            {
                return GenericResult.Failure(OrganizationAccessLog.HolderNotRefreshed(_logger, roleName, organizationId, userId, replaced.CurrentMessage));
            }
        }

        OrganizationAccessLog.RoleRedefined(_logger, roleName, organizationId, holders.Value.Count);
        return GenericResult.Success();
    }
}
