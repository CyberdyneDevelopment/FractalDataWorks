namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A groups request that passed its checks: which grouping was asked for, and the one category or tag the answer is limited to, if any.</summary>
/// <param name="GroupBy">One of <see cref="LineageGroups.ByCategory"/> and <see cref="LineageGroups.ByTag"/>.</param>
/// <param name="Category">The category's name; set only when grouping by category.</param>
/// <param name="Tag">The tag's name; set only when grouping by tag.</param>
/// <param name="ContainersTakeStoreTags">Whether a container takes its data store's tags (storeTags) or sits in its data store path group (storePath); chosen by the caller when grouping by tag.</param>
internal sealed record LineageGroupsAsk(string GroupBy, string? Category, string? Tag, bool ContainersTakeStoreTags);
