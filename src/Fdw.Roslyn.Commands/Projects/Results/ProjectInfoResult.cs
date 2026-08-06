using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Contains detailed information about a project.
/// </summary>
public sealed class ProjectInfoResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectInfoResult"/> class.
    /// </summary>
    public ProjectInfoResult(
        string name,
        string filePath,
        string language,
        string outputKind,
        string languageVersion,
        string nullableContextOptions,
        bool allowUnsafe,
        int documentCount,
        int additionalDocumentCount,
        IReadOnlyList<string> projectReferences,
        IReadOnlyList<string> metadataReferences)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Language = language ?? throw new ArgumentNullException(nameof(language));
        OutputKind = outputKind ?? throw new ArgumentNullException(nameof(outputKind));
        LanguageVersion = languageVersion ?? throw new ArgumentNullException(nameof(languageVersion));
        NullableContextOptions = nullableContextOptions ?? throw new ArgumentNullException(nameof(nullableContextOptions));
        AllowUnsafe = allowUnsafe;
        DocumentCount = documentCount;
        AdditionalDocumentCount = additionalDocumentCount;
        ProjectReferences = projectReferences ?? throw new ArgumentNullException(nameof(projectReferences));
        MetadataReferences = metadataReferences ?? throw new ArgumentNullException(nameof(metadataReferences));
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
    /// Gets the output kind.
    /// </summary>
    public string OutputKind { get; }

    /// <summary>
    /// Gets the language version.
    /// </summary>
    public string LanguageVersion { get; }

    /// <summary>
    /// Gets the nullable context options.
    /// </summary>
    public string NullableContextOptions { get; }

    /// <summary>
    /// Gets a value indicating whether unsafe code is allowed.
    /// </summary>
    public bool AllowUnsafe { get; }

    /// <summary>
    /// Gets the document count.
    /// </summary>
    public int DocumentCount { get; }

    /// <summary>
    /// Gets the additional document count.
    /// </summary>
    public int AdditionalDocumentCount { get; }

    /// <summary>
    /// Gets the project references.
    /// </summary>
    public IReadOnlyList<string> ProjectReferences { get; }

    /// <summary>
    /// Gets the metadata references.
    /// </summary>
    public IReadOnlyList<string> MetadataReferences { get; }
}
