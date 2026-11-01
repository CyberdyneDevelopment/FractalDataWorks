namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Represents a method declaration.
/// </summary>
public sealed class MethodDeclaration
{
    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the return type.
    /// </summary>
    public required string ReturnType { get; init; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public required int Line { get; init; }
}