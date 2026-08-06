using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Top-level result of a MoveProjects command containing all computed changes.
/// </summary>
public sealed class MoveProjectsResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveProjectsResult"/> class.
    /// </summary>
    public MoveProjectsResult(
        IReadOnlyList<ProjectMoveDetail> projectMoves,
        IReadOnlyList<CsprojChangeDetail> csprojChanges,
        SlnxChangeDetail slnxChanges)
    {
        ProjectMoves = projectMoves ?? throw new ArgumentNullException(nameof(projectMoves));
        CsprojChanges = csprojChanges ?? throw new ArgumentNullException(nameof(csprojChanges));
        SlnxChanges = slnxChanges ?? throw new ArgumentNullException(nameof(slnxChanges));
    }

    /// <summary>
    /// Gets the list of project move details.
    /// </summary>
    public IReadOnlyList<ProjectMoveDetail> ProjectMoves { get; }

    /// <summary>
    /// Gets the list of .csproj files that need reference path updates.
    /// </summary>
    public IReadOnlyList<CsprojChangeDetail> CsprojChanges { get; }

    /// <summary>
    /// Gets the .slnx file changes.
    /// </summary>
    public SlnxChangeDetail SlnxChanges { get; }
}
