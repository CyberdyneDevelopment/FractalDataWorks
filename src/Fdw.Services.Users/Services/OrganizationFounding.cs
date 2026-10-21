using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Users.Services;

/// <summary>
/// Makes an organization together with its starting roles and its first administrator (FDW-1758).
/// </summary>
public sealed class OrganizationFounding : IOrganizationFounding
{
    private readonly IOrganizationWriter _organizations;
    private readonly IOrganizationProvider _provider;
    private readonly IOrganizationBootstrap _bootstrap;
    private readonly IUserConfigurationProvider _users;
    private readonly ITenantProvider _tenants;
    private readonly ILogger<OrganizationFounding> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationFounding"/> class.</summary>
    /// <param name="organizations">Makes and removes the organization.</param>
    /// <param name="provider">Reads the organization whose roles are copied.</param>
    /// <param name="bootstrap">Copies the roles and gives the first administrator theirs.</param>
    /// <param name="users">Finds the first administrator's account.</param>
    /// <param name="tenants">Answers whether the first administrator may act in the tenant.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationFounding(
        IOrganizationWriter organizations,
        IOrganizationProvider provider,
        IOrganizationBootstrap bootstrap,
        IUserConfigurationProvider users,
        ITenantProvider tenants,
        ILogger<OrganizationFounding>? logger)
    {
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _bootstrap = bootstrap ?? throw new ArgumentNullException(nameof(bootstrap));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _logger = logger ?? NullLogger<OrganizationFounding>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Found(
        NewOrganization organization, Guid copyRolesFrom, Guid administratorId, string roleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        var checkedInput = await Check(organization.TenantId, copyRolesFrom, administratorId, cancellationToken).ConfigureAwait(false);
        if (checkedInput.IsFailure)
        {
            return checkedInput.ToNewResult<Guid>();
        }

        var made = await _organizations.Create(organization, cancellationToken).ConfigureAwait(false);
        if (made.IsFailure)
        {
            return made;
        }

        var organizationId = made.Value;
        var copied = await _bootstrap.CopyRoles(organization.TenantId, copyRolesFrom, organizationId, cancellationToken).ConfigureAwait(false);
        if (copied.IsFailure)
        {
            OrganizationFoundingLog.RolesNotCopied(_logger, copyRolesFrom, organizationId, copied.CurrentMessage);
            await Undo(organization.TenantId, organizationId, cancellationToken).ConfigureAwait(false);
            return copied.ToNewResult<Guid>();
        }

        var given = await _bootstrap.GiveRole(organization.TenantId, organizationId, administratorId, roleName, cancellationToken).ConfigureAwait(false);
        if (given.IsFailure)
        {
            OrganizationFoundingLog.RoleNotGiven(_logger, administratorId, roleName, organizationId, given.CurrentMessage);
            await Undo(organization.TenantId, organizationId, cancellationToken).ConfigureAwait(false);
            return given.ToNewResult<Guid>();
        }

        OrganizationFoundingLog.Founded(_logger, organizationId, organization.TenantId, administratorId, roleName);
        return made;
    }

    // What is shown before anything is written: the first administrator has an account and may act in the tenant, and the organization whose
    // roles are copied belongs to the same tenant.
    private async Task<IGenericResult> Check(Guid tenantId, Guid copyRolesFrom, Guid administratorId, CancellationToken cancellationToken)
    {
        var person = await _users.Get(administratorId, cancellationToken).ConfigureAwait(false);
        if (!person.IsSuccess || person.Value is null)
        {
            return GenericResult.Failure(OrganizationFoundingLog.AdministratorNotFound(_logger, administratorId));
        }

        // Why the tenant provider and not a lookup of the membership by name: it is the platform's one answer to "may this person act in this
        // tenant", the one sign-in and the tenant switch use, so a membership seeded under another name, or the global tenant's members, count.
        var access = await _tenants.ValidateTenantAccess(tenantId, administratorId.ToString(), cancellationToken).ConfigureAwait(false);
        if (!access.IsSuccess || !access.Value)
        {
            return GenericResult.Failure(OrganizationFoundingLog.AdministratorNotInTenant(_logger, administratorId, tenantId));
        }

        var template = await _provider.Get(copyRolesFrom, cancellationToken).ConfigureAwait(false);
        if (!template.IsSuccess || template.Value is null)
        {
            return GenericResult.Failure(OrganizationFoundingLog.TemplateNotFound(_logger, copyRolesFrom));
        }

        return template.Value.TenantId == tenantId
            ? GenericResult.Success()
            : GenericResult.Failure(OrganizationFoundingLog.TemplateInOtherTenant(_logger, tenantId, copyRolesFrom));
    }

    // Removes what Found made, newest first: the roles and members, then the organization. Each failure is logged as critical and does not
    // stop the next removal, so as little as possible is left behind.
    private async Task Undo(Guid tenantId, Guid organizationId, CancellationToken cancellationToken)
    {
        var cleared = await _bootstrap.Clear(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            OrganizationFoundingLog.NotUndone(_logger, organizationId, "roles and members", cleared.CurrentMessage);
        }

        var removed = await _organizations.Delete(organizationId, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            OrganizationFoundingLog.NotUndone(_logger, organizationId, "organization", removed.CurrentMessage);
        }
    }
}
