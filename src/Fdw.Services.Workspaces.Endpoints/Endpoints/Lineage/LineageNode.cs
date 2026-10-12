namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data set, data store or pipeline in the lineage graph; the workspace is the one the request was scoped to, when it was.</summary>
internal sealed record LineageNode(string Id, string Kind, string Name, string? Workspace);
