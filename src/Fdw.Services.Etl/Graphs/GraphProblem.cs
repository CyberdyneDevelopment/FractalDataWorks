using System.Collections.Generic;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One thing wrong with a stored graph (FDW-1396): where it is, as a path through the graph the API states, and why.
/// </summary>
/// <param name="Path">
/// The path to the offending part, each step one name or index: <c>["linkCapacity"]</c>, <c>["components", "map"]</c>,
/// <c>["components", "keep", "settings", "mappings"]</c>, <c>["components", "split", "outputs", "rest"]</c>, <c>["links", "2"]</c>.
/// An empty path concerns the graph as a whole.
/// </param>
/// <param name="Reason">What is wrong, in words written for the person building the graph.</param>
public sealed record GraphProblem(IReadOnlyList<string> Path, string Reason);
