namespace Fdw.Services.Data.Graphs;

/// <summary>
/// The limits of a read of a Graph data set (FDW-5291): how many rows its graph may produce and how long it may run.
/// </summary>
/// <remarks>
/// Configuration, never code: the defaults are declared once on <see cref="GraphDataSetRunnerServiceType"/> and a host changes them at
/// registration or in configuration. A value that is not a positive number refuses the read by name; it is not replaced by another.
/// </remarks>
/// <param name="MaxRows">The most rows a read may produce.</param>
/// <param name="MaxRunSeconds">The most seconds a read may run.</param>
/// <param name="LinkCapacity">The batches a link of the graph buffers before a writer waits.</param>
public sealed record GraphDataSetReadOptions(int MaxRows, int MaxRunSeconds, int LinkCapacity);
