namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A data set a pipeline touches, and whether it writes it (FDW-1161).</summary>
/// <param name="Name">The data set's name.</param>
/// <param name="Writes">True when the pipeline writes the data set; false when it reads it.</param>
public sealed record ReachedDataSet(string Name, bool Writes);
