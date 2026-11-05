using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data set, data store, container or pipeline in the lineage graph; the workspace is the one the request was scoped to, when it was.</summary>
/// <remarks>
/// The placement is what a group is made of: a data set's category path, a container's data store and path; null for a node with no group, or a data set under no category.
/// A container also names the data store it is in, whose tags it can take.
/// </remarks>
internal sealed record LineageNode(string Id, string Kind, string Name, string? Workspace, string? Placement = null, Guid? DataStoreId = null);
