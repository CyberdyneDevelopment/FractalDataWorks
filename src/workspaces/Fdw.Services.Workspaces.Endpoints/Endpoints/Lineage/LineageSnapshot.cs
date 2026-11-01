using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Reads the map's nodes and edges for a scope and shapes them into a lineage graph.</summary>
/// <remarks>The lineage draws no variations and no category, so neither the data sets' variations (the one read of every variation row the map would otherwise start with) nor the category tree are read.</remarks>
internal static class LineageSnapshot
{
    /// <summary>Reads the whole scope; a read that failed part way is a failure, never a partial graph.</summary>
    public static async Task<IGenericResult<LineageGraph>> Load(
        MapSelection selection, MapSources sources, MapEditing editing, ILogger logger, CancellationToken ct)
    {
        var stream = new OverviewMapStream(selection, sources, editing, logger, readVariations: false, readCategories: false);
        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(ct).WithCancellation(ct).ConfigureAwait(false))
            nodes.Add(node);

        var edges = new List<MapEdgeDto>(stream.Edges());
        return stream.State.Outcome.IsFailure
            ? stream.State.Outcome.ToNewResult<LineageGraph>()
            : GenericResult<LineageGraph>.Success(LineageProjection.Of(nodes, edges, stream.Scope.Workspace));
    }
}
