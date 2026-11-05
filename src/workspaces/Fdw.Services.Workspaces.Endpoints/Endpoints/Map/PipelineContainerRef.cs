namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A container a pipeline reads or writes, as the pipeline names it: by data store, by path within it when it names one, and by container name.</summary>
/// <param name="Store">The name of the data store.</param>
/// <param name="Path">The name of the path within the store; null when the pipeline names none.</param>
/// <param name="Name">The container's name.</param>
internal sealed record PipelineContainerRef(string Store, string? Path, string Name);
