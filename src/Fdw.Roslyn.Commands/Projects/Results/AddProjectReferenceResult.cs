using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Result of adding a project reference.
/// </summary>
public sealed class AddProjectReferenceResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddProjectReferenceResult"/> class.
    /// </summary>
    public AddProjectReferenceResult(string projectName, string referenceName, bool added, string? reason = null)
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        ReferenceName = referenceName ?? throw new ArgumentNullException(nameof(referenceName));
        Added = added;
        Reason = reason;
    }

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the reference name.
    /// </summary>
    public string ReferenceName { get; }

    /// <summary>
    /// Gets a value indicating whether the reference was added.
    /// </summary>
    public bool Added { get; }

    /// <summary>
    /// Gets the reason if the reference was not added.
    /// </summary>
    public string? Reason { get; }
}
