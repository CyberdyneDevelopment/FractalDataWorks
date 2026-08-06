using System;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Represents a file or reference path change captured for the migration guide.
/// </summary>
public sealed class PathChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathChange"/> class.
    /// </summary>
    public PathChange(string oldPath, string newPath, string kind)
    {
        OldPath = oldPath ?? throw new ArgumentNullException(nameof(oldPath));
        NewPath = newPath ?? throw new ArgumentNullException(nameof(newPath));
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
    }

    /// <summary>
    /// Gets the original path.
    /// </summary>
    public string OldPath { get; }

    /// <summary>
    /// Gets the new path.
    /// </summary>
    public string NewPath { get; }

    /// <summary>
    /// Gets the kind of path change (e.g. "Project", "CsprojReference", "SlnxProject").
    /// </summary>
    public string Kind { get; }
}
