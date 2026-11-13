namespace Fdw.Services.Data.Graphs;

/// <summary>
/// How deep a read of a Graph data set may nest other data sets (FDW-5299).
/// </summary>
/// <remarks>
/// Configuration, never code: the default is declared once on <see cref="GraphDataSetRunnerServiceType"/> and a host changes it at registration or in configuration. A value that is
/// not a positive number refuses the read by name; it is not replaced by another.
/// </remarks>
/// <param name="MaxDepth">The most levels of data sets that may sit below the Graph data set being read: a Graph over a Simple data set is 1, a Graph over a Graph over a Simple is 2.</param>
public sealed record GraphDataSetNestingOptions(int MaxDepth);
