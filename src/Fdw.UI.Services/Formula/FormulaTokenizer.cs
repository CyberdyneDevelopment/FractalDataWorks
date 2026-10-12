using System;
using System.Collections.Generic;
using Fdw.Conventions;

namespace Fdw.UI.Services.Formula;

/// <summary>
/// Tokenizes formula expressions for syntax highlighting.
/// Uses character-by-character parsing (no regex per the spec).
/// </summary>
public sealed class FormulaTokenizer : IFormulaTokenizer
{
    private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Aggregate functions
        "SUM", "AVG", "COUNT", "MIN", "MAX", "FIRST", "LAST", "STDEV", "VAR",
        // Logical functions
        "IF", "IIF", "CASE", "WHEN", "THEN", "ELSE", "END", "AND", "OR", "NOT",
        "ISNULL", "ISEMPTY", "COALESCE", "NULLIF",
        // String functions
        "CONCAT", "SUBSTRING", "LEFT", "RIGHT", "LEN", "LENGTH", "TRIM", "LTRIM", "RTRIM",
        "UPPER", "LOWER", "REPLACE", "CHARINDEX", "PATINDEX", "STUFF", "REVERSE", "FORMAT",
        // Math functions
        "ABS", "ROUND", "FLOOR", "CEILING", "POWER", "SQRT", "LOG", "LOG10", "EXP",
        "SIGN", "MOD", "PI", "SIN", "COS", "TAN", "ASIN", "ACOS", "ATAN", "DEGREES", "RADIANS",
        // Date functions
        "GETDATE", "DATEADD", "DATEDIFF", "DATENAME", "DATEPART", "YEAR", "MONTH", "DAY",
        "HOUR", "MINUTE", "SECOND", "DATETRUNC", "EOMONTH",
        // Conversion functions
        "CAST", "CONVERT", "TRY_CAST", "TRY_CONVERT", "PARSE", "TRY_PARSE",
        // Other
        "DISTINCT", "AS", "OVER", "PARTITION", "BY", "ORDER", "ASC", "DESC",
        "BETWEEN", "IN", "LIKE", "ESCAPE", "EXISTS", "NULL", "TRUE", "FALSE"
    };

    private static readonly HashSet<char> OperatorChars = new HashSet<char>
    {
        '+', '-', '*', '/', '%', '=', '<', '>', '!', '&', '|', '^', '~'
    };

    private static readonly HashSet<char> PunctuationChars = new HashSet<char>
    {
        '(', ')', '[', ']', ',', '.', ';', ':'
    };

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 90, MaxMethodLines = 200)]  // Formula tokenizer — comprehensive character-by-character parsing (whitespace, newlines, field refs, strings, numbers, operators, keywords, punctuation, comments)
    public IEnumerable<Token> Tokenize(string formula)
    {
        if (string.IsNullOrEmpty(formula))
        {
            yield break;
        }

        var line = 1;
        var column = 1;
        var index = 0;

        while (index < formula.Length)
        {
            var ch = formula[index];

            // Handle newlines
            if (ch == '\n')
            {
                index++;
                line++;
                column = 1;
                continue;
            }

            if (ch == '\r')
            {
                index++;
                if (index < formula.Length && formula[index] == '\n')
                {
                    index++;
                }
                line++;
                column = 1;
                continue;
            }

            // Handle whitespace (excluding newlines)
            if (char.IsWhiteSpace(ch))
            {
                var startColumn = column;
                var startIndex = index;
                while (index < formula.Length && char.IsWhiteSpace(formula[index]) && formula[index] != '\n' && formula[index] != '\r')
                {
                    index++;
                    column++;
                }
                yield return new Token(TokenType.Whitespace, formula.Substring(startIndex, index - startIndex), line, startColumn);
                continue;
            }

            // Handle field references [DataSet.Field]
            if (ch == '[')
            {
                var startColumn = column;
                var startIndex = index;
                index++;
                column++;

                while (index < formula.Length && formula[index] != ']')
                {
                    if (formula[index] == '\n' || formula[index] == '\r')
                    {
                        break;
                    }
                    index++;
                    column++;
                }

                if (index < formula.Length && formula[index] == ']')
                {
                    index++;
                    column++;
                }

                yield return new Token(TokenType.FieldReference, formula.Substring(startIndex, index - startIndex), line, startColumn);
                continue;
            }

            // Handle string literals 'text'
            if (ch == '\'')
            {
                var startColumn = column;
                var startIndex = index;
                index++;
                column++;

                while (index < formula.Length)
                {
                    if (formula[index] == '\'')
                    {
                        // Check for escaped quote ''
                        if (index + 1 < formula.Length && formula[index + 1] == '\'')
                        {
                            index += 2;
                            column += 2;
                        }
                        else
                        {
                            index++;
                            column++;
                            break;
                        }
                    }
                    else if (formula[index] == '\n' || formula[index] == '\r')
                    {
                        break;
                    }
                    else
                    {
                        index++;
                        column++;
                    }
                }

                yield return new Token(TokenType.String, formula.Substring(startIndex, index - startIndex), line, startColumn);
                continue;
            }

            // Handle double-quoted strings "text"
            if (ch == '"')
            {
                var startColumn = column;
                var startIndex = index;
                index++;
                column++;

                while (index < formula.Length)
                {
                    if (formula[index] == '"')
                    {
                        // Check for escaped quote ""
                        if (index + 1 < formula.Length && formula[index + 1] == '"')
                        {
                            index += 2;
                            column += 2;
                        }
                        else
                        {
                            index++;
                            column++;
                            break;
                        }
                    }
                    else if (formula[index] == '\n' || formula[index] == '\r')
                    {
                        break;
                    }
                    else
                    {
                        index++;
                        column++;
                    }
                }

                yield return new Token(TokenType.String, formula.Substring(startIndex, index - startIndex), line, startColumn);
                continue;
            }

            // Handle comments --
            if (ch == '-' && index + 1 < formula.Length && formula[index + 1] == '-')
            {
                var startColumn = column;
                var startIndex = index;

                while (index < formula.Length && formula[index] != '\n' && formula[index] != '\r')
                {
                    index++;
                    column++;
                }

                yield return new Token(TokenType.Comment, formula.Substring(startIndex, index - startIndex), line, startColumn);
                continue;
            }

            // Handle block comments /* */
            if (ch == '/' && index + 1 < formula.Length && formula[index + 1] == '*')
            {
                var startColumn = column;
                var startIndex = index;
                index += 2;
                column += 2;

                while (index < formula.Length - 1)
                {
                    if (formula[index] == '\n')
                    {
                        line++;
                        column = 1;
                        index++;
                    }
                    else if (formula[index] == '\r')
                    {
                        index++;
                        if (index < formula.Length && formula[index] == '\n')
                        {
                            index++;
                        }
                        line++;
                        column = 1;
                    }
                    else if (formula[index] == '*' && formula[index + 1] == '/')
                    {
                        index += 2;
                        column += 2;
                        break;
                    }
                    else
                    {
                        index++;
                        column++;
                    }
                }

                yield return new Token(TokenType.Comment, formula.Substring(startIndex, index - startIndex), line, startColumn);
                continue;
            }

            // Handle numbers
            if (char.IsDigit(ch) || (ch == '.' && index + 1 < formula.Length && char.IsDigit(formula[index + 1])))
            {
                var startColumn = column;
                var startIndex = index;
                var hasDecimal = ch == '.';

                if (hasDecimal)
                {
                    index++;
                    column++;
                }

                while (index < formula.Length && char.IsDigit(formula[index]))
                {
                    index++;
                    column++;
                }

                if (!hasDecimal && index < formula.Length && formula[index] == '.')
                {
                    index++;
                    column++;
                    while (index < formula.Length && char.IsDigit(formula[index]))
                    {
                        index++;
                        column++;
                    }
                }

                // Handle scientific notation
                if (index < formula.Length && (formula[index] == 'e' || formula[index] == 'E'))
                {
                    index++;
                    column++;
                    if (index < formula.Length && (formula[index] == '+' || formula[index] == '-'))
                    {
                        index++;
                        column++;
                    }
                    while (index < formula.Length && char.IsDigit(formula[index]))
                    {
                        index++;
                        column++;
                    }
                }

                yield return new Token(TokenType.Number, formula.Substring(startIndex, index - startIndex), line, startColumn);
                continue;
            }

            // Handle identifiers and keywords
            if (char.IsLetter(ch) || ch == '_' || ch == '@')
            {
                var startColumn = column;
                var startIndex = index;

                while (index < formula.Length && (char.IsLetterOrDigit(formula[index]) || formula[index] == '_'))
                {
                    index++;
                    column++;
                }

                var text = formula.Substring(startIndex, index - startIndex);
                var tokenType = Keywords.Contains(text) ? TokenType.Keyword : TokenType.Identifier;

                yield return new Token(tokenType, text, line, startColumn);
                continue;
            }

            // Handle operators
            if (OperatorChars.Contains(ch))
            {
                var startColumn = column;
                var startIndex = index;

                // Handle multi-character operators
                if (index + 1 < formula.Length)
                {
                    var next = formula[index + 1];
                    var twoChar = new string(new[] { ch, next });
                    if (string.Equals(twoChar, "<=", StringComparison.Ordinal) ||
                        string.Equals(twoChar, ">=", StringComparison.Ordinal) ||
                        string.Equals(twoChar, "<>", StringComparison.Ordinal) ||
                        string.Equals(twoChar, "!=", StringComparison.Ordinal) ||
                        string.Equals(twoChar, "==", StringComparison.Ordinal) ||
                        string.Equals(twoChar, "&&", StringComparison.Ordinal) ||
                        string.Equals(twoChar, "||", StringComparison.Ordinal) ||
                        string.Equals(twoChar, "+=", StringComparison.Ordinal) ||
                        string.Equals(twoChar, "-=", StringComparison.Ordinal))
                    {
                        index += 2;
                        column += 2;
                        yield return new Token(TokenType.Operator, twoChar, line, startColumn);
                        continue;
                    }
                }

                index++;
                column++;
                yield return new Token(TokenType.Operator, ch.ToString(), line, startColumn);
                continue;
            }

            // Handle punctuation
            if (PunctuationChars.Contains(ch))
            {
                yield return new Token(TokenType.Punctuation, ch.ToString(), line, column);
                index++;
                column++;
                continue;
            }

            // Unknown character
            yield return new Token(TokenType.Unknown, ch.ToString(), line, column);
            index++;
            column++;
        }
    }
}
