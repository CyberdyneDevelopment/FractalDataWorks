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
/// The place above a workspace is the organization its configuration names, or its tenant when it names none (FDW-2338). A workspace that names
/// neither sits nowhere in the tree, which is a fault in the configuration and is not answered with a guess.
/// </summary>
public sealed class WorkspacePlaceParentSource : IPlaceParentSource
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly ILogger<WorkspacePlaceParentSource> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspacePlaceParentSource"/> class.</summary>
    /// <param name="workspaces">Reads the workspaces.</param>
    /// <param name="logger">The logger.</param>
    public WorkspacePlaceParentSource(IWorkspaceConfigurationProvider workspaces, ILogger<WorkspacePlaceParentSource>? logger)
    {
        _workspaces = workspaces;
        _logger = logger ?? NullLogger<WorkspacePlaceParentSource>.Instance;
    }

    /// <inheritdoc />
    public string Kind => PlaceKinds.Workspace;

    /// <inheritdoc />
    public async Task<IGenericResult<PlaceRef?>> Above(Guid placeId, CancellationToken cancellationToken = default)
    {
        var read = await _workspaces.Get(placeId, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<PlaceRef?>();

        if (read.Value is not { IsDeleted: false } workspace)
            return GenericResult<PlaceRef?>.Failure(PlaceAncestryLog.WorkspaceNotFound(_logger, placeId.ToString("D")));

        // A workspace is always in an organization (FDW-3668); a row that names none is data nothing has migrated, and it fails here
        // rather than being read as sitting directly under its tenant.
        return workspace.OrganizationId != Guid.Empty
            ? GenericResult<PlaceRef?>.Success(new PlaceRef(PlaceKinds.Organization, workspace.OrganizationId))
            : GenericResult<PlaceRef?>.Failure(PlaceAncestryLog.WorkspaceSitsNowhere(_logger, placeId.ToString("D")));
    }
}
