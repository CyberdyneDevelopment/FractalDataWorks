using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Users.Services;

/// <summary>
/// Makes a tenant with its default organization and, when the setting <c>FoundingMakesDefaultWorkspace</c> is on, that organization's default
/// workspace, appoints its first administrator, and removes what was made again when any step fails (FDW-1578, FDW-3668, FDW-4600).
/// </summary>
/// <remarks>
/// The data layer has no transaction across these writes, so the order is: judge everything that can be judged first (the person exists,
/// the caller may give the role, the setting), make the tenant, its default organization and, with the setting on, that organization's default
/// workspace (owned by the administrator), add the membership, give the role. With the setting off the organization names no workspace and none is made. A failure of any step after the tenant removes what was made, newest first; removing
/// the tenant removes its organization rows. A removal that itself fails is logged as critical, naming what is left, and the original failure is still the
/// one reported. The membership is named {userId}:{tenantId}, as the claim provisioning names it.
/// </remarks>
public sealed class TenantFounding : ITenantFounding
{
    private const string UserTenantsDomain = "UserTenants";

    private readonly ITenantWriter _tenants;
    private readonly IOrganizationWriter _organizations;
    private readonly IDefaultWorkspaceFounding _defaultWorkspaces;
    private readonly IWorkspaceFoundingPolicy _policy;
    private readonly IUserConfigurationProvider _users;
    private readonly IUserTenantConfigurationProvider _memberships;
    private readonly IGrantRule _grantRule;
    private readonly IUserRoleAssignment _roles;
    private readonly ILogger<TenantFounding> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantFounding"/> class.</summary>
    /// <param name="tenants">Makes and removes the tenant.</param>
    /// <param name="organizations">Makes the tenant's default organization.</param>
    /// <param name="defaultWorkspaces">Makes and removes the default organization's default workspace.</param>
    /// <param name="policy">Says whether founding makes a default workspace.</param>
    /// <param name="users">Finds the person.</param>
    /// <param name="memberships">Writes the person's membership of the tenant.</param>
    /// <param name="grantRule">Judges whether the caller may give the role.</param>
    /// <param name="roles">Gives the role.</param>
    /// <param name="logger">The logger.</param>
    public TenantFounding(
        ITenantWriter tenants,
        IOrganizationWriter organizations,
        IDefaultWorkspaceFounding defaultWorkspaces,
        IWorkspaceFoundingPolicy policy,
        IUserConfigurationProvider users,
        IUserTenantConfigurationProvider memberships,
        IGrantRule grantRule,
        IUserRoleAssignment roles,
        ILogger<TenantFounding>? logger)
    {
        _tenants = tenants;
        _organizations = organizations;
        _defaultWorkspaces = defaultWorkspaces;
        _policy = policy;
        _users = users;
        _memberships = memberships;
        _grantRule = grantRule;
        _roles = roles;
        _logger = logger ?? NullLogger<TenantFounding>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Found(NewTenant tenant, Guid administratorId, string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return GenericResult<Guid>.Failure(TenantFoundingLog.FounderRoleNameRequired(_logger));

        // The founder names the slug of the tenant's first organization; there is no default (FDW-3838). The organization itself is named after the
        // tenant (FDW-4225). Refused before anything is made.
        if (string.IsNullOrWhiteSpace(tenant.OrganizationSlug))
        {
            return GenericResult<Guid>.Failure(TenantFoundingLog.OrganizationNotStated(_logger, tenant.Slug));
        }

        var person = await _users.Get(administratorId, cancellationToken).ConfigureAwait(false);
        if (!person.IsSuccess || person.Value is null)
        {
            return GenericResult<Guid>.Failure(TenantFoundingLog.AdministratorNotFound(_logger, administratorId));
        }

        var allowed = await _grantRule.CanGiveInNewTenant([roleName], cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.ToNewResult<Guid>();
        }

        // Judged before anything is made: whether this founding makes the default workspace. No setting row, host or option stating it is a coded failure.
        var makesWorkspace = _policy.FoundingMakesDefaultWorkspace();
        if (makesWorkspace.IsFailure)
        {
            return makesWorkspace.ToNewResult<Guid>();
        }

        var made = await _tenants.Create(tenant, cancellationToken).ConfigureAwait(false);
        if (made.IsFailure)
        {
            return made;
        }

        var tenantId = made.Value;
        var defaults = await FoundDefaults(tenant, tenantId, administratorId, makesWorkspace.Value, cancellationToken).ConfigureAwait(false);
        if (!defaults.IsSuccess)
        {
            await Undo(tenantId, null, null, cancellationToken).ConfigureAwait(false);
            return defaults.ToNewResult<Guid>();
        }

        var workspace = defaults.Value;

        var membershipName = $"{administratorId}:{tenantId}";
        var member = await _memberships.Save(
            new UserTenantImplementationConfiguration { Name = membershipName, UserId = administratorId, TenantId = tenantId, IsDefault = false },
            UserTenantsDomain, UserTenantsDomain, membershipName, cancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            TenantFoundingLog.MembershipNotSaved(_logger, administratorId, tenantId, member.CurrentMessage);
            await Undo(tenantId, null, workspace, cancellationToken).ConfigureAwait(false);
            return member.ToNewResult<Guid>();
        }

        var given = await _roles.Assign(administratorId, roleName, tenantId, cancellationToken).ConfigureAwait(false);
        if (given.IsFailure)
        {
            TenantFoundingLog.RoleNotGiven(_logger, administratorId, roleName, tenantId, given.CurrentMessage);
            await Undo(tenantId, membershipName, workspace, cancellationToken).ConfigureAwait(false);
            return given.ToNewResult<Guid>();
        }

        TenantFoundingLog.Founded(_logger, tenantId, administratorId, roleName);
        return made;
    }

    // Makes the tenant's default organization and, with the setting on, that organization's default workspace, the workspace the organization row
    // names (FDW-3668). The administrator owns the workspace. With the setting off the organization names none and the result carries no workspace
    // (FDW-4600). A failure is logged here; the caller removes the tenant, which removes the organization rows.
    private async Task<IGenericResult<NewDefaultWorkspace?>> FoundDefaults(
        NewTenant tenant, Guid tenantId, Guid administratorId, bool makesWorkspace, CancellationToken cancellationToken)
    {
        // The workspace's durable id is stated here, by the founding that makes the workspace, and written on the organization row as given.
        Guid? workspaceId = makesWorkspace ? Guid.CreateVersion7() : null;
        var made = await _organizations.Create(
            new NewOrganization(tenantId, tenant.Name, tenant.OrganizationSlug, IsDefault: true, DefaultWorkspaceId: workspaceId), cancellationToken).ConfigureAwait(false);
        if (made.IsFailure)
        {
            TenantFoundingLog.DefaultsNotMade(_logger, tenantId, "default organization", made.CurrentMessage);
            return made.ToNewResult<NewDefaultWorkspace?>();
        }

        if (workspaceId is not { } id)
        {
            return GenericResult<NewDefaultWorkspace?>.Success(null);
        }

        var request = new NewDefaultWorkspace(id, tenantId, tenant.Name, made.Value, administratorId);
        var founded = await _defaultWorkspaces.Found(request, cancellationToken).ConfigureAwait(false);
        if (founded.IsFailure)
        {
            TenantFoundingLog.DefaultsNotMade(_logger, tenantId, "default workspace", founded.CurrentMessage);
            return founded.ToNewResult<NewDefaultWorkspace?>();
        }

        return GenericResult<NewDefaultWorkspace?>.Success(request);
    }

    // Removes what Found made, newest first: the membership when it was saved, the default workspace when it was made, then the tenant, whose
    // removal takes its organization rows with it. Each failure is logged as critical and does not stop the next removal, so as little as
    // possible is left behind.
    private async Task Undo(Guid tenantId, string? membershipName, NewDefaultWorkspace? workspace, CancellationToken cancellationToken)
    {
        if (membershipName is not null)
        {
            var membership = await _memberships.Delete(membershipName, cancellationToken).ConfigureAwait(false);
            if (membership.IsFailure)
            {
                TenantFoundingLog.NotUndone(_logger, tenantId, "membership " + membershipName, membership.CurrentMessage);
            }
        }

        if (workspace is not null)
        {
            var removed = await _defaultWorkspaces.Remove(workspace, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                TenantFoundingLog.NotUndone(_logger, tenantId, "default workspace " + workspace.WorkspaceId, removed.CurrentMessage);
            }
        }

        var tenant = await _tenants.Delete(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            TenantFoundingLog.NotUndone(_logger, tenantId, "tenant", tenant.CurrentMessage);
        }
    }
}
