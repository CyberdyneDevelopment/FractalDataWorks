namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents a type dependency.
/// </summary>
public sealed class TypeDependency
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
    /// Gets or sets the type kind.
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public required string Namespace { get; init; }
}