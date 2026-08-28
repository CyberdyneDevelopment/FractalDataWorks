using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Factory for creating <see cref="IRoslynWorkspace"/> instances.
/// </summary>
/// <remarks>
/// This factory handles MSBuild registration and provides methods for creating
/// workspaces from solution files or empty workspaces.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class RoslynWorkspaceFactory : IRoslynWorkspaceFactory
{
    private static bool _msbuildRegistered;
    private static readonly Lock MsBuildLock = new();

    private readonly ILogger<RoslynWorkspaceFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceFactory"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public RoslynWorkspaceFactory(ILogger<RoslynWorkspaceFactory>? logger = null)
    {
        _logger = logger ?? NullLogger<RoslynWorkspaceFactory>.Instance;
    }

    /// <summary>
    /// Creates a workspace from a solution file.
    /// </summary>
    /// <param name="solutionPath">The path to the .sln file.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A new workspace containing the loaded solution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="solutionPath"/> is null or empty.</exception>
    public Task<IRoslynWorkspace> CreateFromSolution(
        string solutionPath,
        CancellationToken cancellationToken = default)
    {
        return CreateFromSolution(solutionPath, [], cancellationToken);
    }

    /// <summary>
    /// Creates a workspace from a solution file with project filtering.
    /// </summary>
    /// <param name="solutionPath">The path to the .sln file.</param>
    /// <param name="excludePatterns">
    /// Glob patterns for projects to exclude (e.g., "*.Tests", "*.Benchmarks").
    /// Projects matching any pattern will not be loaded initially.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A new workspace containing the loaded solution with filtered projects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="solutionPath"/> is null or empty.</exception>
    public async Task<IRoslynWorkspace> CreateFromSolution(
        string solutionPath,
        IReadOnlyList<string> excludePatterns,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(solutionPath))
            throw new ArgumentNullException(nameof(solutionPath));

        EnsureMSBuildRegistered();

        RoslynWorkspaceLog.SolutionOpening(_logger, solutionPath);

        using var msbuildWorkspace = MSBuildWorkspace.Create();

        var loadDiagnostics = new List<string>();
        msbuildWorkspace.RegisterWorkspaceFailedHandler(args =>
        {
            loadDiagnostics.Add(args.Diagnostic.Message);
            RoslynWorkspaceLog.WorkspaceWarning(_logger, args.Diagnostic.Message);
        });

        var fullSolution = await msbuildWorkspace.OpenSolutionAsync(
            solutionPath,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        fullSolution = fullSolution.WithoutUnresolvedAnalyzers();

        var totalProjects = fullSolution.ProjectIds.Count;

        // If no exclude patterns, return full solution
        if (excludePatterns is null || excludePatterns.Count == 0)
        {
            RoslynWorkspaceLog.SolutionOpened(_logger, solutionPath, totalProjects);
            return new RoslynWorkspace(fullSolution, fullSolution, solutionPath, [], loadDiagnostics);
        }

        // Filter out excluded projects
        var filteredSolution = fullSolution;
        var excludedCount = 0;

        foreach (var project in fullSolution.Projects.ToList())
        {
            if (ShouldExcludeProject(project.Name, excludePatterns))
            {
                filteredSolution = filteredSolution.RemoveProject(project.Id);
                excludedCount++;
                RoslynWorkspaceLog.ProjectExcluded(_logger, project.Name);
            }
        }

        var loadedCount = totalProjects - excludedCount;
        RoslynWorkspaceLog.SolutionOpenedWithFiltering(_logger, solutionPath, loadedCount, excludedCount);

        return new RoslynWorkspace(filteredSolution, fullSolution, solutionPath, excludePatterns, loadDiagnostics);
    }

    /// <summary>
    /// Creates an empty workspace for ad-hoc analysis or testing.
    /// </summary>
    /// <returns>A new empty workspace.</returns>
    public IRoslynWorkspace CreateEmpty()
    {
        var adhocWorkspace = new AdhocWorkspace();
        RoslynWorkspaceLog.EmptyWorkspaceCreated(_logger);
        return new RoslynWorkspace(adhocWorkspace.CurrentSolution);
    }

    /// <summary>
    /// Determines if a project should be excluded based on patterns.
    /// </summary>
    private static bool ShouldExcludeProject(string projectName, IReadOnlyList<string> excludePatterns)
    {
        foreach (var pattern in excludePatterns)
        {
            if (MatchesPattern(projectName, pattern))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Matches a project name against a glob pattern.
    /// </summary>
    private static bool MatchesPattern(string name, string pattern)
    {
        // Convert glob pattern to regex
        // Supports: * (any chars), ? (single char)
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(name, regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Ensures MSBuild is registered for solution loading.
    /// </summary>
    /// <remarks>
    /// MSBuild registration must happen exactly once per process before any
    /// MSBuildWorkspace operations. This method is thread-safe.
    /// </remarks>
    private static void EnsureMSBuildRegistered()
    {
        if (_msbuildRegistered) return;

        lock (MsBuildLock)
        {
            if (_msbuildRegistered) return;

            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();
            _msbuildRegistered = true;
        }
    }
}
