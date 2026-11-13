using System.Collections.Generic;

namespace Fdw.Services.Etl.Graphs;

/// <summary>What judging a graph found (FDW-5457): the problems a save is refused for, and the warnings of what the check cannot judge, which do not stop a save.</summary>
/// <param name="Problems">The problems, each with its path; a save is refused when there is any.</param>
/// <param name="Warnings">The warnings, each with its path; the graph is saved as it would be without them.</param>
public sealed record GraphFindings(IReadOnlyList<GraphProblem> Problems, IReadOnlyList<GraphProblem> Warnings);
