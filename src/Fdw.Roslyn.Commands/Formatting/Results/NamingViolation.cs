namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Represents a naming convention violation.
/// </summary>
public sealed class NamingViolation
{
    /// <summary>
    /// Gets or sets the symbol name.
    /// </summary>
    public string SymbolName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of symbol.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue description.
    /// </summary>
    public string Issue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suggested name.
    /// </summary>
    public string SuggestedName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int Line { get; set; }
}