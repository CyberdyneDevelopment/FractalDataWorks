using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Factory interface for creating <see cref="IRoslynWorkspace"/> instances.
/// </summary>
public interface IRoslynWorkspaceFactory
{
    /// <summary>
    /// Creates a workspace from a solution file.
    /// </summary>
    /// <param name="solutionPath">The path to the .sln file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A new workspace containing the loaded solution.</returns>
    Task<IRoslynWorkspace> CreateFromSolution(
        string solutionPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a workspace from a solution file with project filtering.
    /// </summary>
    /// <param name="solutionPath">The path to the .sln file.</param>
    /// <param name="excludePatterns">
    /// Glob patterns for projects to exclude (e.g., "*.Tests", "*.Benchmarks").
    /// Projects matching any pattern will not be loaded initially.
    /// Use <see cref="DefaultExcludePatterns.TestProjects"/> to exclude common test projects.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A new workspace containing the loaded solution with filtered projects.</returns>
    Task<IRoslynWorkspace> CreateFromSolution(
        string solutionPath,
        IReadOnlyList<string> excludePatterns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an empty workspace for ad-hoc analysis or testing.
    /// </summary>
    /// <returns>A new empty workspace.</returns>
    IRoslynWorkspace CreateEmpty();
}
