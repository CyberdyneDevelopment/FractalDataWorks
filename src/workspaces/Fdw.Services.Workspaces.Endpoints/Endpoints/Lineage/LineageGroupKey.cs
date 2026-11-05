namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Which group a node sits in: its id and kind, its name and the group above it.</summary>
/// <param name="Id">The group's id.</param>
/// <param name="Kind">One of <see cref="LineageGroups"/>'s kinds.</param>
/// <param name="Name">The group's name.</param>
/// <param name="Parent">The id of the group above; null at the top.</param>
internal sealed record LineageGroupKey(string Id, string Kind, string Name, string? Parent);
