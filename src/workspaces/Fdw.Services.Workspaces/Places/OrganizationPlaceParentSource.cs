using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The place above an organization is its tenant, which the organization's own configuration names (FDW-2538).
/// </summary>
public sealed class OrganizationPlaceParentSource : IPlaceParentSource
{
    private readonly IOrganizationProvider _organizations;
    private readonly ILogger<OrganizationPlaceParentSource> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrganizationPlaceParentSource"/> class.</summary>
    /// <param name="organizations">Reads the organizations.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationPlaceParentSource(IOrganizationProvider organizations, ILogger<OrganizationPlaceParentSource>? logger)
    {
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _logger = logger ?? NullLogger<OrganizationPlaceParentSource>.Instance;
    }

    /// <inheritdoc />
    public string Kind => PlaceKinds.Organization;

    /// <inheritdoc />
    public async Task<IGenericResult<PlaceRef?>> Above(Guid placeId, CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.Get(placeId, cancellationToken).ConfigureAwait(false);
        if (organization.IsFailure)
            return organization.ToNewResult<PlaceRef?>();

        return organization.Value is { IsDeleted: false } found
            ? GenericResult<PlaceRef?>.Success(new PlaceRef(PlaceKinds.Tenant, found.TenantId))
            : GenericResult<PlaceRef?>.Failure(PlaceAncestryLog.OrganizationNotFound(_logger, placeId.ToString("D")));
    }
}
