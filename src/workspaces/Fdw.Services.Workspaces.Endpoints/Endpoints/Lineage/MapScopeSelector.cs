using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
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

    /// <summary>
    /// Gets the workspace each data set sits in, by the data set's id (FDW-4345): the workspace that owns it, read from the workspaces the caller may view
    /// in one paged read, whatever scope the map is drawn for.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>
    /// The data sets owned by a workspace the caller may view. A workspace that only uses or produces a data set is not its workspace, and a data set owned by
    /// none, or by one the caller may not view, is absent. A data set owned by several workspaces (the table allows it) is named by the first the read reaches.
    /// </returns>
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, MapWorkspaceRef>>> DataSetWorkspaces(CancellationToken ct)
    {
        var owns = WorkspaceResourceRelationships.ByName("Owns").Name;
        var found = new Dictionary<Guid, MapWorkspaceRef>();
        var skip = 0;
        while (true)
        {
            var page = await _workspaces.Get(skip, MapSources.PageSize, ConfigurationLevels.Children, ct).ConfigureAwait(false);
            if (page.IsFailure) return page.ToNewResult<IReadOnlyDictionary<Guid, MapWorkspaceRef>>();

            await foreach (var workspace in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
            {
                var permitted = await _access.MayView(workspace, ct).ConfigureAwait(false);
                if (permitted.IsFailure)
                {
                    if (WorkspaceAccessRefusals.Hides(permitted)) continue;
                    return permitted.ToNewResult<IReadOnlyDictionary<Guid, MapWorkspaceRef>>();
                }

                foreach (var resource in workspace.Resources.Where(r =>
                    string.Equals(r.ResourceType, MapKinds.DataSet, StringComparison.Ordinal) && string.Equals(r.Relationship, owns, StringComparison.Ordinal)))
                    found.TryAdd(resource.ResourceId, new MapWorkspaceRef(workspace.Id, workspace.Name));
            }

            if (page.Value.Outcome.IsFailure) return page.Value.Outcome.ToNewResult<IReadOnlyDictionary<Guid, MapWorkspaceRef>>();
            if (!page.Value.HasMore) return GenericResult<IReadOnlyDictionary<Guid, MapWorkspaceRef>>.Success(found);

            skip += MapSources.PageSize;
        }
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
