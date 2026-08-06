namespace Fdw.UI.Services.Formula;

/// <summary>
/// Represents a token in a formula expression.
/// </summary>
public sealed class Token
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    public Token(TokenType type, string value, int line, int column)
    {
        Type = type;
        Value = value;
        Line = line;
        Column = column;
        Length = value.Length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class with explicit length.
    /// </summary>
    public Token(TokenType type, string value, int line, int column, int length)
    {
        Type = type;
        Value = value;
        Line = line;
        Column = column;
        Length = length;
    }

    /// <summary>
    /// Gets the token type.
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// Gets the token value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the line number (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column number (1-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the length of the token in the source text.
    /// </summary>
    public int Length { get; }
}
