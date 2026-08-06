namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Represents information about a symbol usage.
/// </summary>
public sealed class UsageInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsageInfo"/> class.
    /// </summary>
    public UsageInfo(
        string filePath,
        int line,
        int column,
        bool isDeclaration = false)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        IsDeclaration = isDeclaration;
    }

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

    /// <summary>
    /// Gets a value indicating whether this is a declaration.
    /// </summary>
    public bool IsDeclaration { get; }
}
