namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents a type hierarchy entry.
/// </summary>
public sealed record TypeHierarchyEntry
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the full type name.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the relationship (BaseType or Interface).
    /// </summary>
    public string Relationship { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the depth in the hierarchy.
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// Gets or sets the type kind.
    /// </summary>
    public string TypeKind { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path (if in source).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number (if in source).
    /// </summary>
    public int? Line { get; init; }
}