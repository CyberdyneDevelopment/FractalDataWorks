using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Result of adding a document to a project.
/// </summary>
public sealed class AddDocumentResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddDocumentResult"/> class.
    /// </summary>
    public AddDocumentResult(string projectName, string documentName, IReadOnlyList<string> folders, bool added, string? reason = null)
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        DocumentName = documentName ?? throw new ArgumentNullException(nameof(documentName));
        Folders = folders ?? throw new ArgumentNullException(nameof(folders));
        Added = added;
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
    /// Gets the folder path.
    /// </summary>
    public IReadOnlyList<string> Folders { get; }

    /// <summary>
    /// Gets a value indicating whether the document was added.
    /// </summary>
    public bool Added { get; }

    /// <summary>
    /// Gets the reason if the document was not added.
    /// </summary>
    public string? Reason { get; }
}
