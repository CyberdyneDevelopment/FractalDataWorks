namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a type collection.
/// </summary>
public sealed class TypeCollectionInfo
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the full type name.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets whether the type is abstract.
    /// </summary>
    public required bool IsAbstract { get; init; }

    /// <summary>
    /// Gets or sets whether the type is partial.
    /// </summary>
    public required bool IsPartial { get; init; }
}