using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Summary information about a document.
/// </summary>
public sealed class DocumentSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSummary"/> class.
    /// </summary>
    public DocumentSummary(string name, string filePath, IReadOnlyList<string> folders)
    {
        Name = name;
        FilePath = filePath;
        Folders = folders;
    }

    /// <summary>
    /// Gets the document name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the document file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the folder path.
    /// </summary>
    public IReadOnlyList<string> Folders { get; }
}