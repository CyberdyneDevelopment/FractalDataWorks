namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// Represents a single change in the workspace.
/// </summary>
public sealed class WorkspaceChange
{
    /// <summary>
    /// Gets or sets the type of change (Added, Modified, Removed).
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path of the changed file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the project name containing the file.
    /// </summary>
    public string Project { get; init; } = string.Empty;
}