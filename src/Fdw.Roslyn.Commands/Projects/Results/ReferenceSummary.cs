using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Summary information about a reference.
/// </summary>
public sealed class ReferenceSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceSummary"/> class.
    /// </summary>
    public ReferenceSummary(string type, string name, string filePath)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <summary>
    /// Gets the reference type (Project or Assembly).
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the reference name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the reference file path.
    /// </summary>
    public string FilePath { get; }
}