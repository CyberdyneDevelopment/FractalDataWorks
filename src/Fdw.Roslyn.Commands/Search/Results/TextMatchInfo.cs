namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Represents information about a text match.
/// </summary>
public sealed class TextMatchInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextMatchInfo"/> class.
    /// </summary>
    public TextMatchInfo(
        string filePath,
        int line,
        int column,
        string matchText,
        string lineText)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        MatchText = matchText;
        LineText = lineText;
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
    /// Gets the matched text.
    /// </summary>
    public string MatchText { get; }

    /// <summary>
    /// Gets the full line text.
    /// </summary>
    public string LineText { get; }
}
