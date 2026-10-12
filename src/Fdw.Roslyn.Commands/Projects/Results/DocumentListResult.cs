using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Contains information about documents in a project.
/// </summary>
public sealed class DocumentListResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentListResult"/> class.
    /// </summary>
    public DocumentListResult(string projectName, int documentCount, IReadOnlyList<DocumentSummary> documents)
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        DocumentCount = documentCount;
        Documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the total number of documents.
    /// </summary>
    public int DocumentCount { get; }

    /// <summary>
    /// Gets the list of documents.
    /// </summary>
    public IReadOnlyList<DocumentSummary> Documents { get; }
}