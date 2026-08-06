using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Result of removing a project reference.
/// </summary>
public sealed class RemoveProjectReferenceResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveProjectReferenceResult"/> class.
    /// </summary>
    public RemoveProjectReferenceResult(string projectName, string referenceName, bool removed, string? reason = null)
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        ReferenceName = referenceName ?? throw new ArgumentNullException(nameof(referenceName));
        Removed = removed;
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
    /// Gets a value indicating whether the reference was removed.
    /// </summary>
    public bool Removed { get; }

    /// <summary>
    /// Gets the reason if the reference was not removed.
    /// </summary>
    public string? Reason { get; }
}
