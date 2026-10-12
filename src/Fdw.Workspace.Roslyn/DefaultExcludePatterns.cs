using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Default exclude patterns for test projects.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public static class DefaultExcludePatterns
{
    /// <summary>
    /// Default patterns to exclude test projects.
    /// </summary>
    public static readonly IReadOnlyList<string> TestProjects =
    [
        "*.Tests",
        "*.UnitTests",
        "*.IntegrationTests",
        "*.Test",
        "*.Specs",
        "*.Benchmarks"
    ];

    /// <summary>
    /// Empty pattern list (load all projects).
    /// </summary>
    public static readonly IReadOnlyList<string> None = [];
}