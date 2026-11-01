using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Roslyn.Commands.Projects.Commands;

namespace Fdw.Roslyn.Commands.Projects.Helpers;

/// <summary>
/// Pure path computation functions for project move operations. No I/O.
/// </summary>
internal static class ProjectPathComputer
{
    /// <summary>
    /// Determines the subfolder of a project relative to the source root.
    /// Returns empty string if the project is directly in the source root.
    /// </summary>
    /// <example>
    /// /abs/public/src/FDW.Foo/FDW.Foo.csproj with sourceRoot /abs/public/src → ""
    /// /abs/public/src/Services/FDW.Foo/FDW.Foo.csproj with sourceRoot /abs/public/src → "Services"
    /// </example>
    internal static string GetCurrentSubfolder(string projectFilePath, string sourceRoot)
    {
        var projectDir = Path.GetDirectoryName(projectFilePath)!;
        var relative = Path.GetRelativePath(sourceRoot, projectDir);

        var separatorIndex = relative.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
        if (separatorIndex < 0)
        {
            return string.Empty;
        }

        return relative.Substring(0, separatorIndex);
    }

    /// <summary>
    /// Computes a new relative Include path for a ProjectReference given the new directories
    /// of both the referencing and referenced projects.
    /// </summary>
    /// <returns>Relative path with backslash separators (csproj convention).</returns>
    internal static string ComputeNewRelativePath(
        string newReferencingDir,
        string newReferencedDir,
        string referencedCsprojFileName)
    {
        var newReferencedCsproj = Path.Combine(newReferencedDir, referencedCsprojFileName);
        var relativePath = Path.GetRelativePath(newReferencingDir, newReferencedCsproj);
        return relativePath.Replace(Path.DirectorySeparatorChar, '\\');
    }

    /// <summary>
    /// Transforms a .slnx project path to reflect a move to a target folder.
    /// </summary>
    /// <example>
    /// "src/X/X.csproj" with targetFolder "Services" → "src/Services/X/X.csproj"
    /// "src/Services/X/X.csproj" with targetFolder "" → "src/X/X.csproj"
    /// </example>
    internal static string ComputeNewSlnxPath(string originalSlnxPath, string targetFolder)
    {
        var fileName = Path.GetFileName(originalSlnxPath);
        var projectDirName = Path.GetFileName(Path.GetDirectoryName(originalSlnxPath)!);

        if (string.IsNullOrEmpty(targetFolder))
        {
            return $"src/{projectDirName}/{fileName}";
        }

        return $"src/{targetFolder}/{projectDirName}/{fileName}";
    }

    /// <summary>
    /// Builds a map of projectName → new directory path for all projects.
    /// Projects not in the moves list keep their current directory.
    /// </summary>
    internal static Dictionary<string, string> BuildProjectDirectoryMap(
        IReadOnlyList<ProjectMoveSpec> moves,
        IReadOnlyDictionary<string, string> currentDirs,
        string sourceRoot)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in currentDirs)
        {
            map[kvp.Key] = kvp.Value;
        }

        foreach (var move in moves)
        {
            if (!currentDirs.TryGetValue(move.ProjectName, out var currentDir))
            {
                continue;
            }

            var projectDirName = Path.GetFileName(currentDir);

            map[move.ProjectName] = string.IsNullOrEmpty(move.TargetFolder)
                ? Path.Combine(sourceRoot, projectDirName)
                : Path.Combine(sourceRoot, move.TargetFolder, projectDirName);
        }

        return map;
    }
}
