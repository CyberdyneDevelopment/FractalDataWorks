namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents nullability information for a symbol.
/// </summary>
public sealed class NullabilitySymbol
{
    /// <summary>
    /// Gets or sets the symbol name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the member kind.
    /// </summary>
    public required string MemberKind { get; init; }

    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets whether the type is nullable.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// Gets or sets the nullable annotation.
    /// </summary>
    public required string NullableAnnotation { get; init; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number.
    /// </summary>
    public required int Column { get; init; }
}