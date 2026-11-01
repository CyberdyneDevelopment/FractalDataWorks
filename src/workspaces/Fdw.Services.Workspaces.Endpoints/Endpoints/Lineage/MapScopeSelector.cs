using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Chooses what a map or lineage request reads: everything the caller can see, one organization, or one workspace.</summary>
internal sealed class MapScopeSelector
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="MapScopeSelector"/> class.</summary>
    public MapScopeSelector(IWorkspaceConfigurationProvider workspaces, IWorkspaceAccessPolicy access, ILogger logger)
    {
        _workspaces = workspaces;
        _access = access;
        _logger = logger;
    }

    /// <summary>Picks the scope the request names; naming two is refused.</summary>
    public async Task<IGenericResult<MapSelection>> Select(string? workspace, Guid? organization, CancellationToken ct)
    {
        if (workspace is not null && organization is not null)
            return MapRequestRefusals.Invalid<MapSelection>(_logger, "name one scope: workspace or organization, not both");

        if (workspace is not null)
        {
            return string.IsNullOrWhiteSpace(workspace)
                ? MapRequestRefusals.Invalid<MapSelection>(_logger, "workspace is blank")
                : await Workspace(workspace, ct).ConfigureAwait(false);
        }

        if (organization is { } organizationId)
        {
            return organizationId == Guid.Empty
                ? MapRequestRefusals.Invalid<MapSelection>(_logger, "organization is the empty id")
                : GenericResult<MapSelection>.Success(new OrganizationMapSelection(organizationId, _logger));
        }

        return GenericResult<MapSelection>.Success(new EverythingMapSelection());
    }

    // A workspace the caller may not view answers exactly as one that does not exist, so a name cannot be probed for
    // (FDW-936). An access decision that could not be made is passed on, not read as "hidden".
    private async Task<IGenericResult<MapSelection>> Workspace(string name, CancellationToken ct)
    {
        var found = await _workspaces.Get(name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found.ToNewResult<MapSelection>();
        if (found.Value is null) return WorkspaceAccessRefusals.Absent<MapSelection>(name, _logger);

        var permitted = await _access.MayView(found.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            return WorkspaceAccessRefusals.Hides(permitted)
                ? WorkspaceAccessRefusals.Absent<MapSelection>(name, _logger)
                : permitted.ToNewResult<MapSelection>();
        }

        // Whether the caller may change the workspace is the policy's own decision, not a guess from the view decision.
        // One that could not be made is passed on, not read as "no".
        var write = await _access.MayWrite(found.Value, ct).ConfigureAwait(false);
        if (write.IsFailure && WorkspaceAccessRefusals.IsUndetermined(write))
            return write.ToNewResult<MapSelection>();

        return GenericResult<MapSelection>.Success(new WorkspaceMapSelection(found.Value, _logger, write.IsSuccess));
    }
}
