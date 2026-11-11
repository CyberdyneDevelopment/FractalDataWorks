using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Details of a single project move operation.
/// </summary>
public sealed class ProjectMoveDetail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectMoveDetail"/> class.
    /// </summary>
    public ProjectMoveDetail(
        string projectName,
        string originalPath,
        string newPath,
        string originalFolder,
        string targetFolder)
    {
        ProjectName = projectName;
        OriginalPath = originalPath;
        NewPath = newPath;
        OriginalFolder = originalFolder;
        TargetFolder = targetFolder;
    }

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the original directory path.
    /// </summary>
    public string OriginalPath { get; }

    /// <summary>
    /// Gets the new directory path after the move.
    /// </summary>
    public string NewPath { get; }

    /// <summary>
    /// Gets the original subfolder relative to src/ (empty string if at root).
    /// </summary>
    public string OriginalFolder { get; }

    /// <summary>
    /// Gets the target subfolder relative to src/.
    /// </summary>
    public string TargetFolder { get; }
}
