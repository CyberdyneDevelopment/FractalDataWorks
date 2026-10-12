using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Summary information about a project.
/// </summary>
public sealed class ProjectSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectSummary"/> class.
    /// </summary>
    public ProjectSummary(string name, string filePath, string language, int documentCount, string outputKind)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Language = language ?? throw new ArgumentNullException(nameof(language));
        DocumentCount = documentCount;
        OutputKind = outputKind ?? throw new ArgumentNullException(nameof(outputKind));
    }

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the project file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the project language.
    /// </summary>
    public string Language { get; }

    /// <summary>
    /// Gets the document count.
    /// </summary>
    public int DocumentCount { get; }

    /// <summary>
    /// Gets the output kind.
    /// </summary>
    public string OutputKind { get; }
}