namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Represents information about an unused member.
/// </summary>
public sealed class UnusedMemberInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnusedMemberInfo"/> class.
    /// </summary>
    public UnusedMemberInfo(
        string name,
        string kind,
        string accessibility,
        string containingType,
        string filePath,
        int line,
        int column)
    {
        Name = name;
        Kind = kind;
        Accessibility = accessibility;
        ContainingType = containingType;
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the member name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the symbol kind.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the accessibility level.
    /// </summary>
    public string Accessibility { get; }

    /// <summary>
    /// Gets the containing type name.
    /// </summary>
    public string ContainingType { get; }

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
