using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What the groups of a graph are made from: the grouping, the limit if any, and the tags of the data sets.</summary>
/// <param name="GroupBy">One of <see cref="LineageGroups.ByCategory"/> and <see cref="LineageGroups.ByTag"/>.</param>
/// <param name="CategoryPath">The path of the category the answer is limited to; null for none. Only when grouping by category.</param>
/// <param name="Tag">The tag the answer is limited to; null for none. Only when grouping by tag.</param>
/// <param name="TagsOfDataSets">The tag names of each data set that carries any, by the data set's id; read only when grouping by tag.</param>
/// <param name="ContainersTakeStoreTags">Whether a container takes the tags of its data store, and so sits in their groups; otherwise it sits in its data store path group. Only when grouping by tag.</param>
/// <param name="MaxPipelinesPerLine">The most pipelines one line names, from the <c>LineageLinePipelines</c> setting.</param>
/// <param name="TagsOfDataStores">The tag names of each data store that carries any, by the data store's id; read only when containers take their data store's tags.</param>
internal sealed record LineageGroupsQuery(
    string GroupBy,
    string? CategoryPath,
    string? Tag,
    IReadOnlyDictionary<Guid, IReadOnlyList<string>> TagsOfDataSets,
    bool ContainersTakeStoreTags,
    IReadOnlyDictionary<Guid, IReadOnlyList<string>> TagsOfDataStores,
    int MaxPipelinesPerLine);
