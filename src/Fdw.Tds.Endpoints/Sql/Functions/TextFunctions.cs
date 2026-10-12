using System;
using Fdw.Protocols.Tds;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// The character arithmetic of SQL Server's string functions: 1-based positions, and the errors SQL
/// Server raises for a negative length.
/// </summary>
public static class TextFunctions
{
    /// <summary>SUBSTRING: the characters from a 1-based start, a start before 1 counting toward the length.</summary>
    /// <param name="call">The call, for the refusal.</param>
    /// <param name="text">The text.</param>
    /// <param name="start">The 1-based start.</param>
    /// <param name="length">How many characters.</param>
    /// <returns>The characters, or error 537 for a negative length.</returns>
    public static IGenericResult<object?> Substring(SqlFunctionCall call, string text, long start, long length)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(text);
        if (length < 0)
            return call.Refuse(new SqlServerMessage(537, 3, 16, "Invalid length parameter passed to the LEFT or SUBSTRING function.", call.Line));

        var from = Math.Max(start, 1);
        var end = Math.Min(start + length - 1, text.Length);
        return SqlFunctionCall.Ok(end < from ? string.Empty : text.Substring((int)from - 1, (int)(end - from + 1)));
    }

    /// <summary>LEFT: the first n characters.</summary>
    /// <param name="call">The call, for the refusal.</param>
    /// <param name="text">The text.</param>
    /// <param name="count">How many characters.</param>
    /// <returns>The characters, or error 536 for a negative count.</returns>
    public static IGenericResult<object?> Left(SqlFunctionCall call, string text, long count)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(text);
        return count < 0
            ? call.Refuse(new SqlServerMessage(536, 1, 16, "Invalid length parameter passed to the left function.", call.Line))
            : SqlFunctionCall.Ok(text[..(int)Math.Min(count, text.Length)]);
    }

    /// <summary>RIGHT: the last n characters.</summary>
    /// <param name="call">The call, for the refusal.</param>
    /// <param name="text">The text.</param>
    /// <param name="count">How many characters.</param>
    /// <returns>The characters, or error 536 for a negative count.</returns>
    public static IGenericResult<object?> Right(SqlFunctionCall call, string text, long count)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(text);
        return count < 0
            ? call.Refuse(new SqlServerMessage(536, 2, 16, "Invalid length parameter passed to the right function.", call.Line))
            : SqlFunctionCall.Ok(text[^(int)Math.Min(count, text.Length)..]);
    }

    /// <summary>CHARINDEX: the 1-based position of one text in another from a start, or 0; matched without case.</summary>
    /// <param name="find">What to find.</param>
    /// <param name="text">Where to look.</param>
    /// <param name="start">The 1-based position to start at; below 1 starts at 1.</param>
    /// <returns>The position, or 0.</returns>
    public static long IndexOf(string find, string text, long start)
    {
        ArgumentNullException.ThrowIfNull(find);
        ArgumentNullException.ThrowIfNull(text);
        var from = (int)Math.Max(start, 1) - 1;
        if (find.Length == 0 || from > text.Length)
            return 0;

        return text.IndexOf(find, from, StringComparison.OrdinalIgnoreCase) + 1;
    }

    /// <summary>QUOTENAME: a name delimited so it reads as one identifier.</summary>
    /// <param name="name">The name.</param>
    /// <param name="quote">The delimiter: [ or ], a single quote, or a double quote.</param>
    /// <returns>The delimited name, or null for a name over 128 characters or another delimiter, as SQL Server answers.</returns>
    public static string? Quote(string name, string quote)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(quote);
        if (name.Length > 128)
            return null;

        return quote switch
        {
            "[" or "]" => "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]",
            "'" => "'" + name.Replace("'", "''", StringComparison.Ordinal) + "'",
            "\"" => "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"",
            _ => null,
        };
    }
}
