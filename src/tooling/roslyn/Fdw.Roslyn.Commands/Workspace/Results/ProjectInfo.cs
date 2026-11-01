namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// Information about a project in the workspace.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of documents in the project.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// Gets or sets the programming language.
    /// </summary>
    public string Language { get; init; } = string.Empty;
}