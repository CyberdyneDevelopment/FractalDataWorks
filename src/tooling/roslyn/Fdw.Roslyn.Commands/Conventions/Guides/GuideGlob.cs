using System;
using System.Text.RegularExpressions;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Matches a name against a pattern in which <c>*</c> stands for any run of characters.
/// </summary>
internal static class GuideGlob
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Determines whether <paramref name="value"/> matches <paramref name="pattern"/> in full.
    /// </summary>
    /// <param name="pattern">The pattern; every other character matches itself.</param>
    /// <param name="value">The name to test.</param>
    /// <returns>True when the whole name matches.</returns>
    public static bool IsMatch(string pattern, string value)
    {
        var expression = "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(value, expression, RegexOptions.CultureInvariant, MatchTimeout);
    }
}
