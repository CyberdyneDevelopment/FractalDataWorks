namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A connection a pipeline names directly, and whether it writes through it (FDW-1161).</summary>
/// <param name="Name">The connection's name.</param>
/// <param name="Writes">True when the pipeline writes through the connection; false when it reads through it.</param>
public sealed record ReachedConnection(string Name, bool Writes);
