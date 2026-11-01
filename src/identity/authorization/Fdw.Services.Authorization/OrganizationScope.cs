using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Says which organization a caller may administer: one that exists, for which they hold the right at the organization's scope
/// (FDW-1593, FDW-2840, FDW-2855).
/// </summary>
/// <remarks>
/// The organization a caller acts in is not compared: acting is a context switch and decides no access. The organization is read first, so a
/// missing one is refused, and the right is then asked of <see cref="IScopeAuthority"/> at the organization's scope, where a right held in
/// the tenant reaches every organization of it, a right an organization grants reaches only that organization, and an organization of a
/// tenant the caller does not act in is refused unless the caller administers the platform. The tenant is judged there, once, and not
/// here: judging it here too would refuse a platform administrator before the authority could admit them. The system context passes.
/// The one thing decided here is what such a refusal looks like: a caller who does not act in the organization's tenant is told the
/// organization is not found, the answer a missing one gets, so no answer says whether an organization of another company exists
/// (FDW-2887). A caller who does act in the tenant and lacks the right is refused for the right: they can already see the organization.
/// </remarks>
public sealed class OrganizationScope : IOrganizationScope
{
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IOrganizationProvider _organizations;
    private readonly IScopeAuthority _authority;
    private readonly ILogger<OrganizationScope> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationScope"/> class.</summary>
    /// <param name="authContext">Supplies the caller and the tenant they act in.</param>
    /// <param name="organizations">Reads an organization, which names its tenant.</param>
    /// <param name="authority">Says whether the caller holds a right at the organization's scope.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationScope(
        IAuthenticationContextAccessor authContext, IOrganizationProvider organizations, IScopeAuthority authority, ILogger<OrganizationScope>? logger)
    {
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _logger = logger ?? NullLogger<OrganizationScope>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<Guid>> TenantOf(Guid organizationId, CancellationToken cancellationToken = default) =>
        Locate(organizationId, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> TenantOf(string right, Guid organizationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(right);

        var located = await Locate(organizationId, cancellationToken).ConfigureAwait(false);
        if (located.IsFailure)
        {
            return located;
        }

        var may = await _authority.May(right, new ThingScope(located.Value, organizationId), cancellationToken).ConfigureAwait(false);
        if (may.IsSuccess)
        {
            return located;
        }

        return ActsIn(located.Value)
            ? may.ToNewResult<Guid>()
            : GenericResult<Guid>.Failure(OrganizationAccessLog.OrganizationNotFound(_logger, organizationId));
    }

    private bool ActsIn(Guid tenantId) => _authContext.Current is { } caller && caller.ActiveTenantId == tenantId;

    // The organization's tenant when a caller is signed in and the organization exists. Which tenants the caller may reach is not asked here.
    private async Task<IGenericResult<Guid>> Locate(Guid organizationId, CancellationToken cancellationToken)
    {
        if (_authContext.Current is not { IsAuthenticated: true })
        {
            return GenericResult<Guid>.Failure(OrganizationAccessLog.CallerRequired(_logger));
        }

        var found = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        return !found.IsSuccess || found.Value is not { IsDeleted: false } organization
            ? GenericResult<Guid>.Failure(OrganizationAccessLog.OrganizationNotFound(_logger, organizationId))
            : GenericResult<Guid>.Success(organization.TenantId);
    }
}
