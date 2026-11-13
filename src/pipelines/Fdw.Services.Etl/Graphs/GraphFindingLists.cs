using System.Collections.Generic;

namespace Fdw.Services.Etl.Graphs;

/// <summary>The lists the steps of one graph add their findings to while it is judged (FDW-5457).</summary>
/// <param name="Problems">The problems a save is refused for.</param>
/// <param name="Warnings">The warnings, which do not stop a save.</param>
internal sealed record GraphFindingLists(List<GraphProblem> Problems, List<GraphProblem> Warnings);
