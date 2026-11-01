using System;
using System.Text;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// Builds the text a SQL <c>LIKE</c> matches for a pattern operator (Contains, StartsWith, EndsWith), and
/// the <c>ESCAPE</c> clause that tells the engine how to read it.
/// </summary>
/// <remarks>
/// <para>
/// The operator states its shape once -- whether anything may come before the value, whether anything may
/// come after it -- and the dialect states what its <c>LIKE</c> reads as syntax and which character escapes
/// it. This joins the two: every metacharacter in the caller's text (and the escape character itself) is
/// escaped, then the wildcards the operator asks for are added around it. The result is a parameter VALUE,
/// never SQL text, so the caller's text cannot become part of the statement, and the same code holds on
/// every connection kind.
/// </para>
/// <para>
/// Why the wildcard is not left to a per-operator SQL fragment: that was T-SQL text
/// (<c>'%' + @p + '%'</c>) which no translator applied, so Contains, StartsWith and EndsWith all matched
/// exactly (FDW-996). String concatenation is also spelled differently in every engine.
/// </para>
/// </remarks>
internal static class LikePattern
{
    /// <summary>The wildcard a pattern operator adds; it is <c>%</c> in every supported dialect.</summary>
    private const char Wildcard = '%';

    /// <summary>Builds the pattern for <paramref name="text"/>.</summary>
    /// <param name="text">The text the caller wants to find.</param>
    /// <param name="dialect">The dialect whose <c>LIKE</c> will read the pattern.</param>
    /// <param name="anythingBefore">Whether any text may precede <paramref name="text"/>.</param>
    /// <param name="anythingAfter">Whether any text may follow <paramref name="text"/>.</param>
    /// <returns>The pattern, ready to be passed as a parameter value.</returns>
    internal static string Build(string text, ISqlDialect dialect, bool anythingBefore, bool anythingAfter)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(dialect);

        var escape = EscapeCharacter(dialect);
        var pattern = new StringBuilder(text.Length + 4);
        if (anythingBefore)
            pattern.Append(Wildcard);

        foreach (var character in text)
        {
            if (character == escape || dialect.LikeMetaCharacters.Contains(character))
                pattern.Append(escape);

            pattern.Append(character);
        }

        if (anythingAfter)
            pattern.Append(Wildcard);

        return pattern.ToString();
    }

    /// <summary>The <c>ESCAPE</c> clause that follows the pattern parameter in the statement.</summary>
    /// <param name="dialect">The dialect the statement is written for.</param>
    /// <returns>For example <c>ESCAPE '!'</c>.</returns>
    internal static string EscapeClause(ISqlDialect dialect)
    {
        ArgumentNullException.ThrowIfNull(dialect);
        return $"ESCAPE '{EscapeCharacter(dialect)}'";
    }

    // A single quote cannot sit bare inside a SQL literal, so a dialect that named it would produce broken
    // SQL for every pattern. That is a defect in the dialect, and it is refused rather than quoted around.
    private static char EscapeCharacter(ISqlDialect dialect)
    {
        var escape = dialect.LikeEscapeCharacter;
        return escape == '\''
            ? throw new InvalidOperationException(
                $"Dialect '{dialect.Name}' names the single quote as its LIKE escape character, which a SQL literal cannot hold.")
            : escape;
    }
}
