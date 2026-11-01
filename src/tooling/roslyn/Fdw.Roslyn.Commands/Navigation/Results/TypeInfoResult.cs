namespace Fdw.Roslyn.Commands.Navigation.Results;

/// <summary>
/// Represents information about a type.
/// </summary>
public sealed record TypeInfoResult
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
    /// Gets or sets the file path where the type is located.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number (1-based) where the type is located.
    /// </summary>
    public int? Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based) where the type is located.
    /// </summary>
    public int? Column { get; init; }

    /// <summary>
    /// Gets or sets the type kind (e.g., Class, Interface, Struct).
    /// </summary>
    public string? TypeKind { get; init; }

    /// <summary>
    /// Gets or sets the accessibility (e.g., Public, Private).
    /// </summary>
    public string? Accessibility { get; init; }

    /// <summary>
    /// Gets or sets the relationship (e.g., BaseClass, Interface).
    /// </summary>
    public string? Relationship { get; init; }
}
