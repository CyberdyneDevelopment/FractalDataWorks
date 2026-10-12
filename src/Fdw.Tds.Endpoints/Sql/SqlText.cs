using System;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql;

/// <summary>
/// The T-SQL text a parsed fragment was written as, for the error that names it.
/// </summary>
public static class SqlText
{
    private const int LongestQuoted = 60;

    /// <summary>Gets the text a fragment was written as, shortened to fit an error message.</summary>
    /// <param name="fragment">The fragment.</param>
    /// <returns>Its text.</returns>
    public static string Of(TSqlFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        if (fragment.ScriptTokenStream is not { } tokens || fragment.FirstTokenIndex < 0)
            return fragment.GetType().Name;

        var text = string.Concat(tokens
            .Skip(fragment.FirstTokenIndex)
            .Take(fragment.LastTokenIndex - fragment.FirstTokenIndex + 1)
            .Select(t => t.Text)).Trim();
        return text.Length > LongestQuoted ? text[..LongestQuoted] + "..." : text;
    }
}
