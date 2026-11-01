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
/// Gives a new organization its starting roles and first administrator, acting for the tenant that owns it (FDW-1758).
/// </summary>
/// <remarks>
/// The roles are copied as they stand, with the same permissions and the same built-in marks; each is judged by the grant rule first, so a
/// caller who does not hold what a role carries copies none of them. Nothing is named here: the roles and the first administrator's role are
/// whatever the organization copied from has and the caller names.
/// </remarks>
public sealed class OrganizationBootstrap : IOrganizationBootstrap
{
    private const string RoleDomain = "OrganizationRole";

    private readonly IOrganizationRoleConfigurationProvider _roles;
    private readonly TenantOrgAccessConfigurationProvider _access;
    private readonly IGrantRule _grantRule;
    private readonly ILogger<OrganizationBootstrap> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationBootstrap"/> class.</summary>
    /// <param name="roles">Reads and writes the roles of organizations.</param>
    /// <param name="access">Reads and writes the people's access rows.</param>
    /// <param name="grantRule">Judges whether the caller holds what a role carries.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationBootstrap(
        IOrganizationRoleConfigurationProvider roles,
        TenantOrgAccessConfigurationProvider access,
        IGrantRule grantRule,
        ILogger<OrganizationBootstrap>? logger)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _grantRule = grantRule ?? throw new ArgumentNullException(nameof(grantRule));
        _logger = logger ?? NullLogger<OrganizationBootstrap>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<OrganizationRoleView>>> CopyRoles(
        Guid tenantId, Guid fromOrganizationId, Guid toOrganizationId, CancellationToken cancellationToken = default)
    {
        var read = await RolesOf(fromOrganizationId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } templates)
        {
            return read.ToNewResult<IReadOnlyList<OrganizationRoleView>>();
        }

        if (templates.Count == 0)
        {
            return GenericResult<IReadOnlyList<OrganizationRoleView>>.Failure(OrganizationBootstrapLog.NothingToCopy(_logger, fromOrganizationId));
        }

        // Why every role is judged before any is written: a refusal must leave the new organization with no roles at all.
        foreach (var template in templates)
        {
            var allowed = _grantRule.CanDefine(PermissionsOf(template), tenantId);
            if (allowed.IsFailure)
            {
                return allowed.ToNewResult<IReadOnlyList<OrganizationRoleView>>();
            }
        }

        foreach (var template in templates)
        {
            var saved = await Save(toOrganizationId, template, cancellationToken).ConfigureAwait(false);
            if (saved.IsFailure)
            {
                return saved.ToNewResult<IReadOnlyList<OrganizationRoleView>>();
            }
        }

        OrganizationBootstrapLog.RolesCopied(_logger, fromOrganizationId, toOrganizationId, templates.Count);
        return GenericResult<IReadOnlyList<OrganizationRoleView>>.Success([.. templates.Select(ViewOf)]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OrganizationMemberChange>> GiveRole(
        Guid tenantId, Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var read = await RolesOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } roles)
        {
            return read.ToNewResult<OrganizationMemberChange>();
        }

        var role = roles.FirstOrDefault(candidate => string.Equals(candidate.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            return GenericResult<OrganizationMemberChange>.Failure(OrganizationAccessLog.RoleNotFound(_logger, organizationId, roleName));
        }

        var permissions = PermissionsOf(role);
        var allowed = _grantRule.CanDefine(permissions, tenantId);
        if (allowed.IsFailure)
        {
            return allowed.ToNewResult<OrganizationMemberChange>();
        }

        var replaced = await _access.Replace(userId, tenantId, organizationId, role.RoleName, permissions, cancellationToken).ConfigureAwait(false);
        if (replaced.IsFailure)
        {
            return replaced.ToNewResult<OrganizationMemberChange>();
        }

        OrganizationAccessLog.MemberGiven(_logger, userId, role.RoleName, organizationId);
        return GenericResult<OrganizationMemberChange>.Success(new OrganizationMemberChange(null, role.RoleName));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Clear(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var members = await _access.Members(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (members.IsFailure || members.Value is not { } rows)
        {
            return members;
        }

        foreach (var userId in rows.Select(row => row.UserId).Distinct())
        {
            var cleared = await _access.Clear(userId, tenantId, organizationId, cancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared;
            }
        }

        var read = await RolesOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } roles)
        {
            return read;
        }

        foreach (var role in roles)
        {
            var removed = await _roles.Delete(NameOf(organizationId, role.RoleName), cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"Role '{role.RoleName}' of organization '{organizationId}'", removed.CurrentMessage));
            }
        }

        OrganizationBootstrapLog.Cleared(_logger, organizationId);
        return GenericResult.Success();
    }

    private static string NameOf(Guid organizationId, string roleName) => $"{organizationId}:{roleName}";

    private static IReadOnlyList<string> PermissionsOf(IOrganizationRoleImplementationConfiguration role)
        => [.. role.Permissions.Select(permission => permission.PermissionName)];

    private static OrganizationRoleView ViewOf(IOrganizationRoleImplementationConfiguration role)
        => new(role.RoleName, role.Description, role.IsBuiltIn, PermissionsOf(role));

    private async Task<IGenericResult<IReadOnlyList<IOrganizationRoleImplementationConfiguration>>> RolesOf(Guid organizationId, CancellationToken cancellationToken)
    {
        var all = await _roles.Get(ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        return all.IsSuccess && all.Value is { } roles
            ? GenericResult<IReadOnlyList<IOrganizationRoleImplementationConfiguration>>.Success(
                [.. roles.Where(role => role.OrganizationId == organizationId && role.IsCurrent && !role.IsDeleted)])
            : GenericResult<IReadOnlyList<IOrganizationRoleImplementationConfiguration>>.Failure(
                OrganizationAccessLog.ReadFailed(_logger, $"The roles of organization '{organizationId}'", all.CurrentMessage));
    }

    private async Task<IGenericResult> Save(Guid organizationId, IOrganizationRoleImplementationConfiguration template, CancellationToken cancellationToken)
    {
        var name = NameOf(organizationId, template.RoleName);
        var saved = await _roles.Save(
            new OrganizationRoleImplementationConfiguration
            {
                Name = name,
                OrganizationId = organizationId,
                RoleName = template.RoleName,
                Description = template.Description,
                IsBuiltIn = template.IsBuiltIn,
                Permissions = [.. template.Permissions.Select(permission => new OrganizationRolePermissionConfiguration { PermissionName = permission.PermissionName })],
            },
            RoleDomain, RoleDomain, name, cancellationToken).ConfigureAwait(false);
        return saved.IsSuccess
            ? saved
            : GenericResult.Failure(OrganizationAccessLog.WriteFailed(_logger, $"Role '{template.RoleName}' of organization '{organizationId}'", saved.CurrentMessage));
    }
}
