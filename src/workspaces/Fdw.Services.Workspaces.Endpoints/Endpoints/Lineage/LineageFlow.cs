namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One step data takes from one node to another; a step through a pipeline names it.</summary>
internal sealed record LineageFlow(string From, string To, string Relation, string? PipelineId);
