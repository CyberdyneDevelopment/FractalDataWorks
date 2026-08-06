namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Represents a duplicate code block location.
/// </summary>
public sealed class DuplicateCodeBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateCodeBlock"/> class.
    /// </summary>
    public DuplicateCodeBlock(
        string filePath,
        string methodName,
        int startLine,
        int endLine,
        int lines,
        int tokens)
    {
        FilePath = filePath;
        MethodName = methodName;
        StartLine = startLine;
        EndLine = endLine;
        Lines = lines;
        Tokens = tokens;
    }

    /// <summary>
    /// Gets the file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the start line.
    /// </summary>
    public int StartLine { get; }

    /// <summary>
    /// Gets the end line.
    /// </summary>
    public int EndLine { get; }

    /// <summary>
    /// Gets the number of lines.
    /// </summary>
    public int Lines { get; }

    /// <summary>
    /// Gets the token count.
    /// </summary>
    public int Tokens { get; }
}
