namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A pipeline the lineage graph passes data through, with whether the caller may edit it.</summary>
internal sealed record LineagePipeline(string Id, string Name, bool? IsEnabled, bool CanEdit);
