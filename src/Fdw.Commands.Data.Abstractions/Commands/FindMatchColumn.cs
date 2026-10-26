using System;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The name of the column a translator adds to a find for each field it searched, which holds 1 when that field matched
/// the search term in the row and 0 when it did not (FDW-1361).
/// </summary>
public static class FindMatchColumn
{
    /// <summary>The prefix every match column carries, so a connection tells it from the container's own columns.</summary>
    public const string Prefix = "__fdw_match_";

    /// <summary>Gets the name of the column that reports whether <paramref name="field"/> matched.</summary>
    /// <param name="field">The searched field's name.</param>
    /// <returns>The match column's name.</returns>
    public static string NameFor(string field) => Prefix + field;

    /// <summary>Gets the field a match column reports on.</summary>
    /// <param name="column">A column's name.</param>
    /// <returns>The searched field's name, or null when <paramref name="column"/> is not a match column.</returns>
    public static string? FieldOf(string column)
        => column.StartsWith(Prefix, StringComparison.Ordinal) ? column.Substring(Prefix.Length) : null;
}
