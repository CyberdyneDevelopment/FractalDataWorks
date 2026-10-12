using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Contains information about references in a project.
/// </summary>
public sealed class ReferenceListResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceListResult"/> class.
    /// </summary>
    public ReferenceListResult(
        string projectName,
        int referenceCount,
        int projectReferenceCount,
        int metadataReferenceCount,
        IReadOnlyList<ReferenceSummary> references)
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        ReferenceCount = referenceCount;
        ProjectReferenceCount = projectReferenceCount;
        MetadataReferenceCount = metadataReferenceCount;
        References = references ?? throw new ArgumentNullException(nameof(references));
    }

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the total number of references.
    /// </summary>
    public int ReferenceCount { get; }

    /// <summary>
    /// Gets the number of project references.
    /// </summary>
    public int ProjectReferenceCount { get; }

    /// <summary>
    /// Gets the number of metadata references.
    /// </summary>
    public int MetadataReferenceCount { get; }

    /// <summary>
    /// Gets the list of references.
    /// </summary>
    public IReadOnlyList<ReferenceSummary> References { get; }
}