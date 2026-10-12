using System;

namespace Fdw.Roslyn.Commands.Analysis.Helpers;

/// <summary>
/// Identifies test projects by name.
/// </summary>
public static class TestProjectDetector
{
    /// <summary>
    /// Determines whether a project name denotes a test project.
    /// </summary>
    /// <param name="projectName">The project name.</param>
    /// <returns><see langword="true"/> when the name denotes a test project.</returns>
    public static bool IsTestProject(string? projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return false;

        return projectName!.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
            || projectName.EndsWith(".Test", StringComparison.OrdinalIgnoreCase)
            || projectName.Contains(".Tests.", StringComparison.OrdinalIgnoreCase)
            || projectName.EndsWith(".IntegrationTests", StringComparison.OrdinalIgnoreCase);
    }
}
