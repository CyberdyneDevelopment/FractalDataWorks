namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents a detected code smell.
/// </summary>
public sealed class CodeSmell
{
    /// <summary>
    /// Gets or sets the smell type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    public required string Member { get; init; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number.
    /// </summary>
    public required int Column { get; init; }
}