using System;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// How close a named thing is to the text a person searched for: the one rule the catalog search and the sources that search at the database
/// share, so a page is in the same order whoever produced its rows.
/// </summary>
public static class SearchRank
{
    /// <summary>The rank of a thing the text does not find.</summary>
    public const int NoMatch = -1;

    /// <summary>Ranks a thing against the text.</summary>
    /// <param name="name">The thing's name.</param>
    /// <param name="description">The thing's description; none for a thing with no description.</param>
    /// <param name="text">What was searched for; empty finds everything equally.</param>
    /// <returns>
    /// 0 when the name is the text, 1 when it starts with it, 2 when it contains it, 3 when only the description contains it, and
    /// <see cref="NoMatch"/> otherwise. Compared without regard to case. With no text everything is rank 0, so a browse lists by name.
    /// </returns>
    public static int Of(string name, string? description, string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.StartsWith(text, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return 2;
        }

        return description is not null && description.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ? 3 : NoMatch;
    }
}
