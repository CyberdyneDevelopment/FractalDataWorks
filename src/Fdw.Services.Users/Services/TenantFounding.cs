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
/// Makes a tenant and appoints its first administrator, and removes the tenant again when the appointment fails (FDW-1578).
/// </summary>
/// <remarks>
/// The data layer has no transaction across these writes, so the order is: judge everything that can be judged first (the person exists,
/// the caller may give the role), make the tenant, add the membership, give the role. A failure of either of the last two removes what
/// was made, newest first. A removal that itself fails is logged as critical, naming what is left, and the original failure is still the
/// one reported. The membership is named {userId}:{tenantId}, as the claim provisioning names it.
/// </remarks>
public sealed class TenantFounding : ITenantFounding
{
    private const string UserTenantsDomain = "UserTenants";

    private readonly ITenantWriter _tenants;
    private readonly IUserConfigurationProvider _users;
    private readonly IUserTenantConfigurationProvider _memberships;
    private readonly IGrantRule _grantRule;
    private readonly IUserRoleAssignment _roles;
    private readonly ILogger<TenantFounding> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantFounding"/> class.</summary>
    /// <param name="tenants">Makes and removes the tenant.</param>
    /// <param name="users">Finds the person.</param>
    /// <param name="memberships">Writes the person's membership of the tenant.</param>
    /// <param name="grantRule">Judges whether the caller may give the role.</param>
    /// <param name="roles">Gives the role.</param>
    /// <param name="logger">The logger.</param>
    public TenantFounding(
        ITenantWriter tenants,
        IUserConfigurationProvider users,
        IUserTenantConfigurationProvider memberships,
        IGrantRule grantRule,
        IUserRoleAssignment roles,
        ILogger<TenantFounding>? logger)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _memberships = memberships ?? throw new ArgumentNullException(nameof(memberships));
        _grantRule = grantRule ?? throw new ArgumentNullException(nameof(grantRule));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _logger = logger ?? NullLogger<TenantFounding>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Found(NewTenant tenant, Guid administratorId, string roleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

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

        var made = await _tenants.Create(tenant, cancellationToken).ConfigureAwait(false);
        if (made.IsFailure)
        {
            return made;
        }

        var tenantId = made.Value;
        var membershipName = $"{administratorId}:{tenantId}";
        var member = await _memberships.Save(
            new UserTenantImplementationConfiguration { Name = membershipName, UserId = administratorId, TenantId = tenantId, IsDefault = false },
            UserTenantsDomain, UserTenantsDomain, membershipName, cancellationToken).ConfigureAwait(false);
        if (member.IsFailure)
        {
            TenantFoundingLog.MembershipNotSaved(_logger, administratorId, tenantId, member.CurrentMessage);
            await Undo(tenantId, null, cancellationToken).ConfigureAwait(false);
            return member.ToNewResult<Guid>();
        }

        var given = await _roles.Assign(administratorId, roleName, tenantId, cancellationToken).ConfigureAwait(false);
        if (given.IsFailure)
        {
            TenantFoundingLog.RoleNotGiven(_logger, administratorId, roleName, tenantId, given.CurrentMessage);
            await Undo(tenantId, membershipName, cancellationToken).ConfigureAwait(false);
            return given.ToNewResult<Guid>();
        }

        TenantFoundingLog.Founded(_logger, tenantId, administratorId, roleName);
        return made;
    }

    // Removes what Found made, newest first: the membership when it was saved, then the tenant. Each failure is logged as critical and
    // does not stop the next removal, so as little as possible is left behind.
    private async Task Undo(Guid tenantId, string? membershipName, CancellationToken cancellationToken)
    {
        if (membershipName is not null)
        {
            var membership = await _memberships.Delete(membershipName, cancellationToken).ConfigureAwait(false);
            if (membership.IsFailure)
            {
                TenantFoundingLog.NotUndone(_logger, tenantId, "membership " + membershipName, membership.CurrentMessage);
            }
        }

        var tenant = await _tenants.Delete(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            TenantFoundingLog.NotUndone(_logger, tenantId, "tenant", tenant.CurrentMessage);
        }
    }
}
