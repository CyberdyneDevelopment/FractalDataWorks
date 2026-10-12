using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Workspaces.Lineage;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Checks lineage requests and refuses each bad one with the reason a caller can act on.</summary>
internal static class LineageRequests
{
    private const string NeighbourhoodKinds = "DataSet,Pipeline";
    private const string SearchKinds = "DataSet,DataStore,Pipeline";
    private const string ContainsMatch = "contains";
    private const string StartsWithMatch = "startsWith";

    /// <summary>Checks a neighbourhood request: the node, the direction and the depth are required.</summary>
    public static IGenericResult<LineageNeighbourhoodQuery> Neighbourhood(LineageNeighbourhoodRequest request, ILogger logger)
    {
        var problem = NodeProblem(request.Node, "node", allowPipeline: true);
        if (problem is null) problem = DepthProblem(request.Depth);
        if (problem is not null) return MapRequestRefusals.Invalid<LineageNeighbourhoodQuery>(logger, problem);

        var direction = Direction(request.Direction);
        if (direction is null)
        {
            return MapRequestRefusals.Invalid<LineageNeighbourhoodQuery>(
                logger, $"direction '{request.Direction}' is not one of {string.Join(", ", LineageDirections.All().Where(d => d.IncludesUpstream || d.IncludesDownstream).Select(d => d.Wire))}; it is required");
        }

        var kinds = LineageKindSelection.Parse(request.Kinds, NeighbourhoodKinds, pipelinesAreNodes: false, logger);
        return kinds.IsFailure
            ? kinds.ToNewResult<LineageNeighbourhoodQuery>()
            : GenericResult<LineageNeighbourhoodQuery>.Success(
                new LineageNeighbourhoodQuery(request.Node!.Trim(), direction, request.Depth!.Value, kinds.Value!));
    }

    /// <summary>Checks a search request: the text is required; the count is held to its maximum.</summary>
    public static IGenericResult<LineageSearchQuery> Search(LineageSearchRequest request, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(request.Q))
            return MapRequestRefusals.Invalid<LineageSearchQuery>(logger, "q is required: the text to find in names");

        var take = LineageLimits.DefaultSearchTake;
        if (request.Take.HasValue) take = request.Take.Value;
        if (take < 1 || take > LineageLimits.MaxSearchTake)
            return MapRequestRefusals.Invalid<LineageSearchQuery>(logger, $"take {take} is outside 1 to {LineageLimits.MaxSearchTake}");

        var skip = request.Skip.HasValue ? request.Skip.Value : 0;
        if (skip < 0)
            return MapRequestRefusals.Invalid<LineageSearchQuery>(logger, $"skip {skip} is below 0");

        var matchProblem = MatchProblem(request.Match, out var startsWith);
        if (matchProblem is not null) return MapRequestRefusals.Invalid<LineageSearchQuery>(logger, matchProblem);

        var kinds = LineageKindSelection.Parse(request.Kinds, SearchKinds, pipelinesAreNodes: true, logger);
        return kinds.IsFailure
            ? kinds.ToNewResult<LineageSearchQuery>()
            : GenericResult<LineageSearchQuery>.Success(new LineageSearchQuery(request.Q.Trim(), kinds.Value!, take, skip, startsWith));
    }

    /// <summary>Checks a path request: both ends are required and are data sets or data stores.</summary>
    public static IGenericResult<LineagePathQuery> Path(LineagePathRequest request, ILogger logger)
    {
        var problem = NodeProblem(request.From, "from", allowPipeline: false);
        if (problem is null) problem = NodeProblem(request.To, "to", allowPipeline: false);
        if (problem is not null) return MapRequestRefusals.Invalid<LineagePathQuery>(logger, problem);

        var maxDepth = LineageLimits.MaxPathDepth;
        if (request.MaxDepth.HasValue) maxDepth = request.MaxDepth.Value;
        if (maxDepth < 1 || maxDepth > LineageLimits.MaxPathDepth)
            return MapRequestRefusals.Invalid<LineagePathQuery>(logger, $"maxDepth {maxDepth} is outside 1 to {LineageLimits.MaxPathDepth}");

        return GenericResult<LineagePathQuery>.Success(new LineagePathQuery(request.From!.Trim(), request.To!.Trim(), maxDepth));
    }

    // Contains is the default; a word that is neither of the two is refused, never read as one of them.
    private static string? MatchProblem(string? match, out bool startsWith)
    {
        startsWith = false;
        if (string.IsNullOrWhiteSpace(match)) return null;

        var wanted = match.Trim();
        if (string.Equals(wanted, StartsWithMatch, StringComparison.OrdinalIgnoreCase))
        {
            startsWith = true;
            return null;
        }

        return string.Equals(wanted, ContainsMatch, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"match '{match}' is not one of {ContainsMatch}, {StartsWithMatch}";
    }

    private static string? NodeProblem(string? node, string parameter, bool allowPipeline)
    {
        if (string.IsNullOrWhiteSpace(node))
        {
            return $"{parameter} is required: the id of a data set or data store{(allowPipeline ? ", or a pipeline" : string.Empty)}, as Kind:id";
        }

        if (!MapNodeIds.TryParse(node.Trim(), out var kind, out _))
            return $"{parameter} '{node}' is not a node id (Kind:id)";

        var traced = kind is MapKinds.DataSet or MapKinds.DataStore || (allowPipeline && kind == MapKinds.Pipeline);
        return traced ? null : $"{parameter} '{node}' is a {kind}, which the lineage does not trace";
    }

    private static string? DepthProblem(int? depth)
    {
        if (depth is null) return $"depth is required: a whole number from 1 to {LineageLimits.MaxDepth}";
        return depth is >= 1 and <= LineageLimits.MaxDepth ? null : $"depth {depth} is outside 1 to {LineageLimits.MaxDepth}";
    }

    // The word must match exactly: a miss is NotFound, which is no direction, so there is no default to fall back to.
    private static ILineageDirection? Direction(string? wire)
    {
        if (string.IsNullOrWhiteSpace(wire)) return null;

        var found = LineageDirections.ByWire(wire.Trim());
        return ReferenceEquals(found, LineageDirections.NotFound) ? null : found;
    }
}
