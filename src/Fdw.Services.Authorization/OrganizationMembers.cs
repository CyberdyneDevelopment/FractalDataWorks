using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// The people in an organization and the one role each holds there, kept as access rows (FDW-1593).
/// </summary>
/// <remarks>
/// A person's rows in an organization are replaced as a whole when they are given a role, so they hold one role there and exactly its
/// permissions. The roles are read through <see cref="IOrganizationRoles"/>, which applies the organization check itself.
/// </remarks>
public sealed class OrganizationMembers : IOrganizationMembers
{
    private readonly IOrganizationScope _scope;
    private readonly IOrganizationRoles _roles;
    private readonly TenantOrgAccessConfigurationProvider _access;
    private readonly IGrantRule _grantRule;
    private readonly ILogger<OrganizationMembers> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationMembers"/> class.</summary>
    /// <param name="scope">Judges whether the caller may administer the organization.</param>
    /// <param name="roles">Reads the roles of the organization.</param>
    /// <param name="access">Reads and writes the access rows.</param>
    /// <param name="grantRule">Judges whether the caller holds what a role carries.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationMembers(
        IOrganizationScope scope,
        IOrganizationRoles roles,
        TenantOrgAccessConfigurationProvider access,
        IGrantRule grantRule,
        ILogger<OrganizationMembers>? logger)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _grantRule = grantRule ?? throw new ArgumentNullException(nameof(grantRule));
        _logger = logger ?? NullLogger<OrganizationMembers>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OrganizationMemberChange>> Give(Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<OrganizationMemberChange>();
        }

        var role = await _roles.Get(organizationId, roleName, cancellationToken).ConfigureAwait(false);
        if (role.IsFailure || role.Value is null)
        {
            return role.ToNewResult<OrganizationMemberChange>();
        }

        // Why before anything is written: the caller may give only what they hold themselves (FDW-1577), and a refusal must leave the
        // person's access as it was.
        var allowed = _grantRule.CanDefine(role.Value.Permissions, tenant.Value);
        if (allowed.IsFailure)
        {
            return allowed.ToNewResult<OrganizationMemberChange>();
        }

        var held = await _access.Get(userId, tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure || held.Value is null)
        {
            return held.ToNewResult<OrganizationMemberChange>();
        }

        var replaced = await _access.Replace(userId, tenant.Value, organizationId, role.Value.Name, role.Value.Permissions, cancellationToken).ConfigureAwait(false);
        if (replaced.IsFailure)
        {
            return replaced.ToNewResult<OrganizationMemberChange>();
        }

        OrganizationAccessLog.MemberGiven(_logger, userId, role.Value.Name, organizationId);
        return GenericResult<OrganizationMemberChange>.Success(new OrganizationMemberChange(held.Value.Count > 0 ? held.Value[0].RoleName : null, role.Value.Name));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Remove(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<string>();
        }

        var held = await _access.Get(userId, tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure || held.Value is null)
        {
            return held.ToNewResult<string>();
        }

        if (held.Value.Count == 0)
        {
            return GenericResult<string>.Failure(OrganizationAccessLog.MemberNotFound(_logger, userId, organizationId));
        }

        var cleared = await _access.Clear(userId, tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            return cleared.ToNewResult<string>();
        }

        OrganizationAccessLog.MemberRemoved(_logger, userId, organizationId);
        return GenericResult<string>.Success(held.Value[0].RoleName);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<OrganizationMember>>> List(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var tenant = await _scope.TenantOf(organizationId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant.ToNewResult<IReadOnlyList<OrganizationMember>>();
        }

        var rows = await _access.Members(tenant.Value, organizationId, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            return rows.ToNewResult<IReadOnlyList<OrganizationMember>>();
        }

        return GenericResult<IReadOnlyList<OrganizationMember>>.Success(
            [.. rows.Value.GroupBy(row => row.UserId).Select(person => new OrganizationMember(person.Key, person.First().RoleName))]);
    }
}
