namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Represents a declared symbol in the semantic model.
/// </summary>
public sealed class SymbolDeclaration
{
    /// <summary>
    /// Gets or sets the symbol name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the symbol kind (NamedType, Method, Property, etc.).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Gets or sets the fully qualified name.
    /// </summary>
    public required string FullyQualifiedName { get; init; }

    /// <summary>
    /// Gets or sets the accessibility (Public, Private, etc.).
    /// </summary>
    public required string Accessibility { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is abstract.
    /// </summary>
    public bool IsAbstract { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is sealed.
    /// </summary>
    public bool IsSealed { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is static.
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Gets or sets the return type (for methods).
    /// </summary>
    public string? ReturnType { get; init; }

    /// <summary>
    /// Gets or sets whether the method is async (for methods).
    /// </summary>
    public bool IsAsync { get; init; }

    /// <summary>
    /// Gets or sets whether the method is virtual (for methods).
    /// </summary>
    public bool IsVirtual { get; init; }

    /// <summary>
    /// Gets or sets whether the method is an override (for methods).
    /// </summary>
    public bool IsOverride { get; init; }
}