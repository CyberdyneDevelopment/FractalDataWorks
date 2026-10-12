using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Extension methods for <see cref="Solution"/>.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public static class SolutionExtensions
{
    /// <summary>
    /// Creates a copy of the solution with all unresolved analyzer references removed.
    /// This prevents errors when using SymbolFinder methods that iterate through analyzer references.
    /// </summary>
    /// <param name="solution">The solution to clean.</param>
    /// <returns>A new solution with only resolved analyzer references.</returns>
    public static Solution WithoutUnresolvedAnalyzers(this Solution solution)
    {
        var cleanSolution = solution;
        foreach (var project in solution.Projects)
        {
            var resolvedRefs = project.AnalyzerReferences
                .Where(r => r is not UnresolvedAnalyzerReference)
                .ToList();

            if (resolvedRefs.Count != project.AnalyzerReferences.Count)
            {
                cleanSolution = cleanSolution.WithProjectAnalyzerReferences(project.Id, resolvedRefs);
            }
        }
        return cleanSolution;
    }
}
