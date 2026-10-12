namespace Fdw.Roslyn.Commands.Navigation.Results;

/// <summary>
/// Represents information about an override.
/// </summary>
public sealed class OverrideInfo
{
    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the containing type name.
    /// </summary>
    public required string ContainingType { get; init; }

    /// <summary>
    /// Gets or sets the full name.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public required int Column { get; init; }
}
