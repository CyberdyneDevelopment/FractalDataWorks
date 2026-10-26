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
/// The tenant is the top of the scope tree: nothing is above it. It is read from the tenant configuration so that a tenant that does not exist is
/// not a place (FDW-2538).
/// </summary>
public sealed class TenantPlaceParentSource : IPlaceParentSource
{
    private readonly ITenantProvider _tenants;
    private readonly ILogger<TenantPlaceParentSource> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantPlaceParentSource"/> class.</summary>
    /// <param name="tenants">Reads the tenants.</param>
    /// <param name="logger">The logger.</param>
    public TenantPlaceParentSource(ITenantProvider tenants, ILogger<TenantPlaceParentSource>? logger)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _logger = logger ?? NullLogger<TenantPlaceParentSource>.Instance;
    }

    /// <inheritdoc />
    public string Kind => PlaceKinds.Tenant;

    /// <inheritdoc />
    public async Task<IGenericResult<PlaceRef?>> Above(Guid placeId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenants.GetTenant(placeId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<PlaceRef?>();

        return tenant.Value is null
            ? GenericResult<PlaceRef?>.Failure(PlaceAncestryLog.TenantNotFound(_logger, placeId.ToString("D")))
            : GenericResult<PlaceRef?>.Success(null);
    }
}
