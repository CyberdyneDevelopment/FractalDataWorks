namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents a reference to a type.
/// </summary>
public sealed class TypeReference
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
    /// Gets or sets the namespace.
    /// </summary>
    public required string Namespace { get; init; }
}