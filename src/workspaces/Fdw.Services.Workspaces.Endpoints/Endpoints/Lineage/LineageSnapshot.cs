using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Reads the map's nodes and edges for a scope and shapes them into a lineage graph.</summary>
/// <remarks>The lineage draws no variations, so the data sets' variations (the one read of every variation row the map would otherwise start with) are not read. The category tree is read only for the groups, which place each data set in its category.</remarks>
internal static class LineageSnapshot
{
    /// <summary>Reads the whole scope; a read that failed part way is a failure, never a partial graph.</summary>
    /// <param name="selection">What the caller may see.</param>
    /// <param name="sources">The providers.</param>
    /// <param name="editing">What the caller may edit.</param>
    /// <param name="readCategories">Whether each data set names the category it sits under; the groups need it, the walks do not.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">The cancellation token.</param>
    public static async Task<IGenericResult<LineageGraph>> Load(
        MapSelection selection, MapSources sources, MapEditing editing, bool readCategories, ILogger logger, CancellationToken ct)
    {
        var stream = new OverviewMapStream(selection, sources, editing, logger, readVariations: false, readCategories: readCategories);
        var nodes = new List<MapNodeDto>();
        await foreach (var node in stream.Nodes(ct).WithCancellation(ct).ConfigureAwait(false))
            nodes.Add(node);

        var edges = new List<MapEdgeDto>(stream.Edges());
        if (stream.State.Outcome.IsSuccess)
            await LineageContainerEnds.Add(stream.State, sources, nodes, edges, logger, ct).ConfigureAwait(false);

        return stream.State.Outcome.IsFailure
            ? stream.State.Outcome.ToNewResult<LineageGraph>()
            : GenericResult<LineageGraph>.Success(LineageProjection.Of(nodes, edges, stream.Scope.Workspace));
    }
}
