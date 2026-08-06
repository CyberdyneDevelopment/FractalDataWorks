using System;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Specifies a single project move: which project and where to move it.
/// </summary>
public sealed class ProjectMoveSpec
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectMoveSpec"/> class.
    /// </summary>
    /// <param name="projectName">The name of the project to move.</param>
    /// <param name="targetFolder">Target folder relative to src/ (e.g., "Services"). Empty string means src/ root.</param>
    public ProjectMoveSpec(string projectName, string targetFolder)
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        TargetFolder = targetFolder ?? throw new ArgumentNullException(nameof(targetFolder));
    }

    /// <summary>
    /// Gets the name of the project to move.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the target folder relative to src/ (e.g., "Services"). Empty string means src/ root.
    /// </summary>
    public string TargetFolder { get; }
}
