namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Represents a file change in a mutation result.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class FileChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileChange"/> class.
    /// </summary>
    /// <param name="filePath">The path of the changed file.</param>
    /// <param name="changeType">The type of change.</param>
    /// <param name="projectName">The name of the project containing the file.</param>
    public FileChange(string filePath, IFileChangeType changeType, string projectName)
    {
        FilePath = filePath;
        ChangeType = changeType;
        ProjectName = projectName;
    }

    /// <summary>
    /// Gets the path of the changed file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public IFileChangeType ChangeType { get; }

    /// <summary>
    /// Gets the name of the project containing the file.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets or sets the number of text changes in the file.
    /// </summary>
    public int TextChangeCount { get; init; }
}
