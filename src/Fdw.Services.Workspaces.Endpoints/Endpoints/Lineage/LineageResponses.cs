using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Shapes lineage walks into the responses the endpoints send.</summary>
internal static class LineageResponses
{
    /// <summary>Reads the pipeline each edge passes through, when pipelines are named at all; one stage-count read per pipeline.</summary>
    public static async Task<IGenericResult<Dictionary<string, LineageViaDto>>> Vias(
        LineageReader reader, LineageGraph graph, List<LineageFlow> flows, bool named, CancellationToken ct)
    {
        var vias = new Dictionary<string, LineageViaDto>(StringComparer.Ordinal);
        if (!named) return GenericResult<Dictionary<string, LineageViaDto>>.Success(vias);

        var ids = flows
            .Where(flow => flow.PipelineId is not null)
            .Select(flow => flow.PipelineId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var counts = await reader.StageCounts(ids, ct).ConfigureAwait(false);
        if (counts.IsFailure) return counts.ToNewResult<Dictionary<string, LineageViaDto>>();

        foreach (var id in ids)
        {
            var pipeline = graph.Pipeline(id);
            if (pipeline is null) continue;

            counts.Value!.TryGetValue(id, out var stages);
            vias[id] = new LineageViaDto { Id = id, Name = pipeline.Name, IsEnabled = pipeline.IsEnabled, StageCount = stages, CanEdit = pipeline.CanEdit };
        }

        return GenericResult<Dictionary<string, LineageViaDto>>.Success(vias);
    }

    /// <summary>Shapes a neighbourhood walk.</summary>
    public static LineageNeighbourhoodResponse Neighbourhood(
        LineageNode focus, LineageWalk walk, LineageKindSelection kinds, Dictionary<string, LineageViaDto> vias) => new()
    {
        Focus = new LineageFocusDto { Id = focus.Id, Kind = focus.Kind, Name = focus.Name },
        Nodes = walk.Nodes.Select(Node).ToList(),
        Edges = walk.Flows.Select(flow => Edge(flow, vias)).ToList(),
        Pipelines = Pipelines(vias),
        Truncated = walk.Truncated,
        MaxNodes = LineageLimits.MaxNodes,
        UnavailableKinds = kinds.Unavailable,
    };

    /// <summary>Shapes a path walk; no walk means no path was found.</summary>
    public static LineagePathResponse Path(LineageWalk? walk, int maxDepth, Dictionary<string, LineageViaDto> vias) => walk is null
        ? new LineagePathResponse { Found = false, MaxDepth = maxDepth }
        : new LineagePathResponse
        {
            Found = true,
            MaxDepth = maxDepth,
            Nodes = walk.Nodes.Select(Node).ToList(),
            Edges = walk.Flows.Select(flow => Edge(flow, vias)).ToList(),
            Pipelines = Pipelines(vias),
        };

    /// <summary>Shapes search matches.</summary>
    public static List<LineageSearchHitDto> Hits(List<LineageNode> nodes)
        => nodes.Select(node => new LineageSearchHitDto { Id = node.Id, Kind = node.Kind, Name = node.Name, Workspace = node.Workspace }).ToList();

    /// <summary>Shapes a page of search matches into the paged envelope, each hit with the path that leads to it.</summary>
    public static LineageSearchPageResponse PagedHits(LineageSearchPage page) => new()
    {
        Skip = page.Skip,
        Take = page.Take,
        HasMore = page.HasMore,
        Items = page.Items.Select(node => HitWithPath(node, page.Root)).ToList(),
    };

    private static LineageSearchHitDto HitWithPath(LineageNode node, LineagePathStepDto? root)
    {
        var path = new List<LineagePathStepDto>();
        if (root is not null) path.Add(root);
        path.Add(new LineagePathStepDto { Id = node.Id, Kind = node.Kind, Name = node.Name });
        return new LineageSearchHitDto { Id = node.Id, Kind = node.Kind, Name = node.Name, Workspace = node.Workspace, Path = path };
    }

    private static LineageNodeDto Node(LineagePlacedNode placed) => new()
    {
        Id = placed.Node.Id,
        Kind = placed.Node.Kind,
        Name = placed.Node.Name,
        Workspace = placed.Node.Workspace,
        Depth = placed.Depth,
    };

    private static LineageEdgeDto Edge(LineageFlow flow, Dictionary<string, LineageViaDto> vias)
    {
        LineageViaDto? via = null;
        if (flow.PipelineId is not null) vias.TryGetValue(flow.PipelineId, out via);
        return new LineageEdgeDto { From = flow.From, To = flow.To, Relation = flow.Relation, Via = via };
    }

    private static List<LineagePipelineDto> Pipelines(Dictionary<string, LineageViaDto> vias)
        => vias.Values
            .OrderBy(via => via.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(via => via.Id, StringComparer.Ordinal)
            .Select(via => new LineagePipelineDto { Id = via.Id, Name = via.Name })
            .ToList();
}
