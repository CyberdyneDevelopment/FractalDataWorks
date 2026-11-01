namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Represents information about a symbol.
/// </summary>
public sealed class SymbolInfoResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolInfoResult"/> class.
    /// </summary>
    public SymbolInfoResult(
        string name,
        string fullName,
        string kind,
        string filePath,
        int line,
        int column)
    {
        Name = name;
        FullName = fullName;
        Kind = kind;
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the symbol name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the full name.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the symbol kind.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the line number.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column number.
    /// </summary>
    public int Column { get; }
}
