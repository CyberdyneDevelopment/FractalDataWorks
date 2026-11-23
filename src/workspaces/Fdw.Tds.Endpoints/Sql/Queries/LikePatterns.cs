using System;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Reads a LIKE pattern as the one filter operator it is: starts with, ends with, contains or equals.
/// </summary>
internal static class LikePatterns
{
    /// <summary>Reads a pattern.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="filterOperator">The operator it is.</param>
    /// <param name="literal">The text it matches, without its % marks.</param>
    /// <returns>False for a pattern with _ or [ ], or with % anywhere but its ends.</returns>
    public static bool TryRead(string pattern, out string filterOperator, out string literal)
    {
        var leading = pattern.StartsWith('%');
        var trailing = pattern.Length > 1 && pattern.EndsWith('%');
        literal = pattern[(leading ? 1 : 0)..(pattern.Length - (trailing ? 1 : 0))];

        var shape = LikeWildcardShapes.Of(leading, trailing);
        filterOperator = shape.FilterOperator;

        return !ReferenceEquals(shape, LikeWildcardShapes.NotFound) && literal.Length > 0 && literal.IndexOfAny(['%', '_', '[']) < 0;
    }
}
