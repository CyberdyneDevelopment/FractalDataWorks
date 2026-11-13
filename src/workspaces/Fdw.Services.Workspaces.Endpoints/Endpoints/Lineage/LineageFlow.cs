namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One step data takes from one node to another; a step through a pipeline names it.</summary>
/// <remarks>
/// An intended step (FDW-5296) is one a Graph data set says it will take once bound: nothing moves along it yet. A Graph step (FDW-5456) is any step of a
/// Graph data set's reads; the groups route draws those and no other step that has no pipeline.
/// </remarks>
internal sealed record LineageFlow(string From, string To, string Relation, string? PipelineId, bool Intended = false, bool Graph = false);
