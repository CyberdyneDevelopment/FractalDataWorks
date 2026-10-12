namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Represents a type declaration.
/// </summary>
public sealed class TypeDeclaration
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the type kind (ClassDeclaration, InterfaceDeclaration, etc.).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public required int Line { get; init; }
}