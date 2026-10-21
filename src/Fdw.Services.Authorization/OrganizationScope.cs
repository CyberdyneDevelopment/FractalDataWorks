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
/// Says which organization a caller may administer: the one they act in, in the tenant they act in (FDW-1593).
/// </summary>
/// <remarks>
/// The organization a caller acts in is the one the request resolved (<see cref="IOrgContext"/>): the <c>orgId</c> claim of the token when
/// it carries one, otherwise the default organization of the tenant. The claim alone is not enough, because a token usually carries none.
/// The check against it comes first and needs no store, so a caller who is told "not here" learns nothing about whether the organization
/// exists. The system context passes.
/// </remarks>
public sealed class OrganizationScope : IOrganizationScope
{
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IOrgContext _orgContext;
    private readonly IOrganizationProvider _organizations;
    private readonly ILogger<OrganizationScope> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationScope"/> class.</summary>
    /// <param name="authContext">Supplies the caller and the tenant they act in.</param>
    /// <param name="orgContext">Supplies the organization the request resolved, which is the one the caller acts in.</param>
    /// <param name="organizations">Reads an organization, which names its tenant.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationScope(
        IAuthenticationContextAccessor authContext, IOrgContext orgContext, IOrganizationProvider organizations, ILogger<OrganizationScope>? logger)
    {
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _orgContext = orgContext ?? throw new ArgumentNullException(nameof(orgContext));
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _logger = logger ?? NullLogger<OrganizationScope>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> TenantOf(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (_authContext.Current is not { IsAuthenticated: true } caller)
        {
            return GenericResult<Guid>.Failure(OrganizationAccessLog.CallerRequired(_logger));
        }

        if (!caller.IsSystemContext && _orgContext.OrgId != organizationId)
        {
            return GenericResult<Guid>.Failure(OrganizationAccessLog.NotActingInOrganization(_logger, organizationId));
        }

        var found = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess || found.Value is null)
        {
            return GenericResult<Guid>.Failure(OrganizationAccessLog.OrganizationNotFound(_logger, organizationId));
        }

        var tenantId = found.Value.TenantId;
        return caller.IsSystemContext || caller.ActiveTenantId == tenantId
            ? GenericResult<Guid>.Success(tenantId)
            : GenericResult<Guid>.Failure(OrganizationAccessLog.OtherTenant(_logger, organizationId, tenantId));
    }
}
