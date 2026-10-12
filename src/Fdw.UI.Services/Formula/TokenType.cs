namespace Fdw.UI.Services.Formula;

/// <summary>
/// Types of tokens in a formula.
/// </summary>
#pragma warning disable FDW017
public enum TokenType
#pragma warning restore FDW017
{
    /// <summary>
    /// Unknown token.
    /// </summary>
    Unknown,

    /// <summary>
    /// Whitespace.
    /// </summary>
    Whitespace,

    /// <summary>
    /// Keyword (SUM, IF, etc.).
    /// </summary>
    Keyword,

    /// <summary>
    /// Identifier (column name, variable).
    /// </summary>
    Identifier,

    /// <summary>
    /// Field reference [DataSet.Field].
    /// </summary>
    FieldReference,

    /// <summary>
    /// String literal.
    /// </summary>
#pragma warning disable CA1720 // Identifier contains type name - acceptable for token type enum
    String,
#pragma warning restore CA1720

    /// <summary>
    /// Numeric literal.
    /// </summary>
    Number,

    /// <summary>
    /// Operator (+, -, *, /, etc.).
    /// </summary>
    Operator,

    /// <summary>
    /// Punctuation (, . ; : ( ) [ ]).
    /// </summary>
    Punctuation,

    /// <summary>
    /// Comment (-- or /* */).
    /// </summary>
    Comment
}
