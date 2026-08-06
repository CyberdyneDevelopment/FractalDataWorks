using System;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// JSON-safe snapshot of a <see cref="FileChange"/> for storage in a <see cref="ChangeLedgerEntry"/>.
/// </summary>
// Why: ChangeLedgerEntry cannot store raw FileChange because FileChange.ChangeType is IFileChangeType
// (not JSON-serializable via the stdio .Data reflection path); this snapshots ChangeType.Name instead.
public sealed class LedgerFileChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerFileChange"/> class.
    /// </summary>
    public LedgerFileChange(string filePath, string changeType, string projectName, int textChangeCount)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        ChangeType = changeType ?? throw new ArgumentNullException(nameof(changeType));
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        TextChangeCount = textChangeCount;
    }

    /// <summary>
    /// Gets the path of the changed file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the change type name (a <see cref="FileChangeTypes"/> option name).
    /// </summary>
    public string ChangeType { get; }

    /// <summary>
    /// Gets the name of the project containing the file.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the number of text changes in the file.
    /// </summary>
    public int TextChangeCount { get; }
}
