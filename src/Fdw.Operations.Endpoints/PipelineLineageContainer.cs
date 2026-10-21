namespace Fdw.Operations.Endpoints;

/// <summary>A data store container a pipeline reads or writes, as the lineage draws it (FDW-1269).</summary>
/// <param name="DataStoreName">The data store the container sits in.</param>
/// <param name="ContainerName">The container's name.</param>
internal sealed record PipelineLineageContainer(string DataStoreName, string ContainerName);
