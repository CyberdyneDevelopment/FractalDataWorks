using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Groups the data sets and containers of a lineage graph and totals the pipelines that run between groups (FDW-3824, FDW-3850). One pass
/// over the graph's nodes and one over its flows: no read of its own, so the cost follows the graph and not the number of pipelines.
/// </summary>
/// <remarks>
/// Grouping by category puts each data set in its category (or Uncategorised). Grouping by tag puts a data set in the group of each tag it
/// carries (or Untagged), so one data set can sit in several groups. A step through a pipeline makes a line for every pair of different groups
/// its two ends sit in; an end pair that shares a group makes no line for that group.
/// </remarks>
internal static class LineageGroups
{
    /// <summary>The grouping that puts each data set in its category.</summary>
    public const string ByCategory = "category";

    /// <summary>The grouping that puts each data set in the group of every tag it carries.</summary>
    public const string ByTag = "tag";

    /// <summary>The choice that keeps a container in its data store path group when grouping by tag.</summary>
    public const string ContainersByStorePath = "storePath";

    /// <summary>The choice that puts a container in the group of each tag its data store carries when grouping by tag.</summary>
    public const string ContainersByStoreTags = "storeTags";

    /// <summary>The kind of group that is a data-set category.</summary>
    public const string CategoryKind = "Category";

    /// <summary>The kind of group that is a tag.</summary>
    public const string TagKind = "Tag";

    /// <summary>The kind of group that is the containers of one data store path.</summary>
    public const string StorePathKind = "StorePath";

    /// <summary>The kind and id of the group for data sets that sit under no category.</summary>
    public const string Uncategorised = "Uncategorised";

    /// <summary>The kind and id of the group for data sets that carry no tag.</summary>
    public const string Untagged = "Untagged";

    private const string CategoryPrefix = "Category:";
    private const string TagPrefix = "Tag:";
    private const string StorePathPrefix = "StorePath:";

    /// <summary>Builds the groups and lines of a graph.</summary>
    /// <param name="graph">What the caller can see.</param>
    /// <param name="query">The grouping, the limit if any, and the tags of the data sets.</param>
    public static LineageGroupsResponse Of(LineageGraph graph, LineageGroupsQuery query)
    {
        var keys = new Dictionary<string, LineageGroupKey>(StringComparer.Ordinal);
        var members = new Dictionary<string, int>(StringComparer.Ordinal);
        var groupsOf = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
        {
            var inScope = KeysOf(node, query).Where(key => InScope(key, query)).ToList();
            if (inScope.Count == 0) continue;

            groupsOf[node.Id] = [.. inScope.Select(key => key.Id)];
            foreach (var key in inScope)
            {
                keys[key.Id] = key;
                members[key.Id] = members.GetValueOrDefault(key.Id) + 1;
            }
        }

        if (string.Equals(query.GroupBy, ByCategory, StringComparison.Ordinal)) AddAncestors(keys, query.CategoryPath);
        return new LineageGroupsResponse { Groups = Groups(keys, members), Lines = Lines(graph, groupsOf, query.MaxPipelinesPerLine) };
    }

    // A data store and a pipeline sit in no group.
    private static IReadOnlyList<LineageGroupKey> KeysOf(LineageNode node, LineageGroupsQuery query)
    {
        if (string.Equals(node.Kind, MapKinds.Container, StringComparison.Ordinal)) return ContainerKeys(node, query);
        if (!string.Equals(node.Kind, MapKinds.DataSet, StringComparison.Ordinal)) return [];

        return string.Equals(query.GroupBy, ByTag, StringComparison.Ordinal) ? TagKeys(DataSetTags(node, query), query.Tag) : [CategoryKeyOf(node.Placement)];
    }

    // A container sits in its data store path, or, when the request chose it, takes the tags of its data store. A container carries no tag of its own,
    // so a tag limit leaves it out unless it takes its data store's.
    private static IReadOnlyList<LineageGroupKey> ContainerKeys(LineageNode node, LineageGroupsQuery query)
    {
        if (string.Equals(query.GroupBy, ByTag, StringComparison.Ordinal) && query.ContainersTakeStoreTags)
        {
            var tags = node.DataStoreId is { } storeId && query.TagsOfDataStores.TryGetValue(storeId, out var found) ? found : [];
            return TagKeys(tags, query.Tag);
        }

        return query.Tag is null && !string.IsNullOrEmpty(node.Placement)
            ? [new LineageGroupKey(StorePathPrefix + node.Placement, StorePathKind, node.Placement, null)]
            : [];
    }

    private static IReadOnlyList<string> DataSetTags(LineageNode node, LineageGroupsQuery query)
        => MapNodeIds.TryParse(node.Id, out _, out var id) && query.TagsOfDataSets.TryGetValue(id, out var found) ? found : [];

    private static LineageGroupKey CategoryKeyOf(string? path)
        => string.IsNullOrEmpty(path) ? new LineageGroupKey(Uncategorised, Uncategorised, Uncategorised, null) : CategoryKey(path);

    // A thing is in the group of each tag it carries, and in Untagged when it carries none. A tag limit keeps the things that carry the tag
    // and groups them by their other tags: the limiting tag would hold every one of them.
    private static IReadOnlyList<LineageGroupKey> TagKeys(IReadOnlyList<string> names, string? limit)
    {
        if (limit is null)
            return names.Count == 0 ? [new LineageGroupKey(Untagged, Untagged, Untagged, null)] : [.. names.Select(TagKey)];

        return names.Contains(limit, StringComparer.OrdinalIgnoreCase)
            ? [.. names.Where(name => !string.Equals(name, limit, StringComparison.OrdinalIgnoreCase)).Select(TagKey)]
            : [];
    }

    private static LineageGroupKey TagKey(string name) => new(TagPrefix + name, TagKind, name, null);

    private static LineageGroupKey CategoryKey(string path)
    {
        var cut = path.LastIndexOf(CategoryTree.PathSeparator, StringComparison.Ordinal);
        return cut < 0
            ? new LineageGroupKey(CategoryPrefix + path, CategoryKind, path, null)
            : new LineageGroupKey(CategoryPrefix + path, CategoryKind, path[(cut + CategoryTree.PathSeparator.Length)..], CategoryPrefix + path[..cut]);
    }

    // A category answer holds the category and the categories under it, and no store path or uncategorised group. Other groupings are not limited here.
    private static bool InScope(LineageGroupKey key, LineageGroupsQuery query)
        => query.CategoryPath is null || (string.Equals(key.Kind, CategoryKind, StringComparison.Ordinal) && InPath(key, query.CategoryPath));

    // A category's parent groups are answered too, so a view can draw the tree; they hold no data set of their own. A scoped answer stops at its category.
    private static void AddAncestors(Dictionary<string, LineageGroupKey> keys, string? categoryPath)
    {
        foreach (var key in keys.Values.ToList())
        {
            var parent = key.Parent;
            while (parent is not null && !keys.ContainsKey(parent))
            {
                var ancestor = CategoryKey(parent[CategoryPrefix.Length..]);
                if (categoryPath is not null && !InPath(ancestor, categoryPath)) break;

                keys[ancestor.Id] = ancestor;
                parent = ancestor.Parent;
            }
        }
    }

    private static bool InPath(LineageGroupKey key, string categoryPath)
    {
        var path = key.Id[CategoryPrefix.Length..];
        return string.Equals(path, categoryPath, StringComparison.Ordinal)
            || path.StartsWith(categoryPath + CategoryTree.PathSeparator, StringComparison.Ordinal);
    }

    private static List<LineageGroupDto> Groups(Dictionary<string, LineageGroupKey> keys, Dictionary<string, int> members)
        => keys.Values
            .OrderBy(key => key.Kind, StringComparer.Ordinal)
            .ThenBy(key => key.Id, StringComparer.Ordinal)
            .Select(key => new LineageGroupDto
            {
                Id = key.Id,
                Kind = key.Kind,
                Name = key.Name,
                Parent = key.Parent is { } parent && keys.ContainsKey(parent) ? parent : null,
                Members = members.GetValueOrDefault(key.Id),
            })
            .ToList();

    // A step through a pipeline joins every pair of different groups its ends sit in; the same pipeline along the same pair counts once however many steps cross it.
    private static List<LineageLineDto> Lines(LineageGraph graph, Dictionary<string, List<string>> groupsOf, int maxPipelines)
    {
        var pipelinesByLine = new Dictionary<(string From, string To), HashSet<string>>();
        foreach (var flow in graph.Flows)
        {
            if (flow.PipelineId is null) continue;
            if (!groupsOf.TryGetValue(flow.From, out var from) || !groupsOf.TryGetValue(flow.To, out var to)) continue;

            foreach (var source in from)
                foreach (var target in to)
                    AddStep(pipelinesByLine, source, target, flow.PipelineId);
        }

        return pipelinesByLine
            .OrderBy(line => line.Key.From, StringComparer.Ordinal)
            .ThenBy(line => line.Key.To, StringComparer.Ordinal)
            .Select(line => Line(graph, line.Key.From, line.Key.To, line.Value, maxPipelines))
            .ToList();
    }

    private static void AddStep(Dictionary<(string From, string To), HashSet<string>> pipelinesByLine, string from, string to, string pipelineId)
    {
        if (string.Equals(from, to, StringComparison.Ordinal)) return;

        if (!pipelinesByLine.TryGetValue((from, to), out var pipelines))
        {
            pipelines = new HashSet<string>(StringComparer.Ordinal);
            pipelinesByLine[(from, to)] = pipelines;
        }

        pipelines.Add(pipelineId);
    }

    private static LineageLineDto Line(LineageGraph graph, string from, string to, HashSet<string> pipelineIds, int maxPipelines) => new()
    {
        From = from,
        To = to,
        Count = pipelineIds.Count,
        Pipelines = pipelineIds
            .Select(id => graph.Pipeline(id))
            .OfType<LineagePipeline>()
            .OrderBy(pipeline => pipeline.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pipeline => pipeline.Id, StringComparer.Ordinal)
            .Take(maxPipelines)
            .Select(pipeline => new LineagePipelineDto { Id = pipeline.Id, Name = pipeline.Name })
            .ToList(),
    };
}
