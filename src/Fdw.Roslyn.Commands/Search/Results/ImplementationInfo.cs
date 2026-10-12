namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Represents information about an implementation.
/// </summary>
public sealed class ImplementationInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplementationInfo"/> class.
    /// </summary>
    public ImplementationInfo(
        string name,
        string fullName,
        string containingType,
        string filePath,
        int line,
        int column)
    {
        Name = name;
        FullName = fullName;
        ContainingType = containingType;
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the full name.
    /// </summary>
    public string FullName { get; }

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
