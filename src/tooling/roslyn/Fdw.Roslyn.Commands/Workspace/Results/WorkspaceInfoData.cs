using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// Data returned from workspace information operations.
/// </summary>
public sealed class WorkspaceInfoData
{
    /// <summary>
    /// Gets or sets the solution file path.
    /// </summary>
    public string SolutionFilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of projects.
    /// </summary>
    public int ProjectCount { get; init; }

    /// <summary>
    /// Gets or sets the number of documents.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// Gets or sets the list of projects.
    /// </summary>
    public IReadOnlyList<ProjectInfo> Projects { get; init; } = System.Array.Empty<ProjectInfo>();
}