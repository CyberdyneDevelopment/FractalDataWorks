namespace Fdw.Roslyn.Commands.Navigation.Results;

/// <summary>
/// Represents a symbol's location information.
/// </summary>
public sealed class SymbolLocationInfo
{
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
