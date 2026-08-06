using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Result of removing a document from a project.
/// </summary>
public sealed class RemoveDocumentResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveDocumentResult"/> class.
    /// </summary>
    public RemoveDocumentResult(string projectName, string documentName, string documentPath, bool removed, string? reason = null)
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        DocumentName = documentName ?? throw new ArgumentNullException(nameof(documentName));
        DocumentPath = documentPath ?? throw new ArgumentNullException(nameof(documentPath));
        Removed = removed;
        Reason = reason;
    }

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the document name.
    /// </summary>
    public string DocumentName { get; }

    /// <summary>
    /// Gets the document file path.
    /// </summary>
    public string DocumentPath { get; }

    /// <summary>
    /// Gets a value indicating whether the document was removed.
    /// </summary>
    public bool Removed { get; }

    /// <summary>
    /// Gets the reason if the document was not removed.
    /// </summary>
    public string? Reason { get; }
}
