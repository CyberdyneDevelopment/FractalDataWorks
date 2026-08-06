using System.Text.RegularExpressions;

namespace Fdw.Services.Notifications.Extensions;

/// <summary>
/// Extension members for HTML string manipulation using C# 14 extension syntax.
/// </summary>
public static partial class StringHtmlExtensions
{
    [GeneratedRegex("<[^>]*>", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HtmlTagRegex();

    extension(string? html)
    {
        /// <summary>
        /// Strips HTML tags from the string.
        /// </summary>
        /// <returns>The string with HTML tags removed.</returns>
        public string StripHtmlTags()
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            return HtmlTagRegex().Replace(html, string.Empty);
        }
    }
}
