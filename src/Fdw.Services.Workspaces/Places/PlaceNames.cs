using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The default <see cref="IPlaceNames"/>: the name of an organization is read from the organization's configuration and the name of a workspace from the
/// workspace's own record (FDW-2620). A place that does not exist, or was deleted, has no name; the tenant is not named here, since the tenant's version is
/// shown under the definition's plain name.
/// </summary>
public sealed class PlaceNames : IPlaceNames
{
    private readonly IOrganizationProvider _organizations;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly ILogger<PlaceNames> _logger;

    /// <summary>Initializes a new instance of the <see cref="PlaceNames"/> class.</summary>
    /// <param name="organizations">Reads the organizations.</param>
    /// <param name="workspaces">Reads the workspaces.</param>
    /// <param name="logger">The logger.</param>
    public PlaceNames(IOrganizationProvider organizations, IWorkspaceConfigurationProvider workspaces, ILogger<PlaceNames>? logger)
    {
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        _logger = logger ?? NullLogger<PlaceNames>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<string>> Get(string placeKind, Guid placeId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(placeKind, PlaceKinds.Organization, StringComparison.Ordinal))
        {
            var organization = await _organizations.Get(placeId, cancellationToken).ConfigureAwait(false);
            if (organization.IsFailure)
            {
                return organization.ToNewResult<string>();
            }

            return organization.Value is { IsDeleted: false } found
                ? GenericResult<string>.Success(found.Name)
                : GenericResult<string>.Failure(PlaceAncestryLog.OrganizationNotFound(_logger, placeId.ToString("D")));
        }

        if (string.Equals(placeKind, PlaceKinds.Workspace, StringComparison.Ordinal))
        {
            var workspace = await _workspaces.Get(placeId, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
            if (workspace.IsFailure)
            {
                return workspace.ToNewResult<string>();
            }

            return workspace.Value is { IsDeleted: false } found
                ? GenericResult<string>.Success(found.Name)
                : GenericResult<string>.Failure(PlaceAncestryLog.WorkspaceNotFound(_logger, placeId.ToString("D")));
        }

        return GenericResult<string>.Failure(PlaceAncestryLog.NoScopeName(_logger, placeKind));
    }
}
