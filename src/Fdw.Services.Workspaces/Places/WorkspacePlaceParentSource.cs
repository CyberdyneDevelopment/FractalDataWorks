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
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
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

        if (workspace.OrganizationId is { } organizationId)
            return GenericResult<PlaceRef?>.Success(new PlaceRef(PlaceKinds.Organization, organizationId));

        return workspace.TenantId is { } tenantId
            ? GenericResult<PlaceRef?>.Success(new PlaceRef(PlaceKinds.Tenant, tenantId))
            : GenericResult<PlaceRef?>.Failure(PlaceAncestryLog.WorkspaceSitsNowhere(_logger, placeId.ToString("D")));
    }
}
