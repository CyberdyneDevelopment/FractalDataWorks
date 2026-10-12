namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A data store container a pipeline touches directly, and whether it writes it (FDW-1161).</summary>
/// <param name="DataStore">The data store's name.</param>
/// <param name="Path">The path within the data store.</param>
/// <param name="Container">The container within the path.</param>
/// <param name="Writes">True when the pipeline writes the container; false when it reads it.</param>
public sealed record ReachedContainer(string DataStore, string Path, string Container, bool Writes);
