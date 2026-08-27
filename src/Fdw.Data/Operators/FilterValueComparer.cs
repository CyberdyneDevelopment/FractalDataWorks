using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Fdw.Data;

/// <summary>
/// Compares a row value against a filter value in memory.
/// </summary>
/// <remarks>
/// Used when the translator cannot express the filter natively and the connector has to prune the
/// rows itself. Kept in one place so a CSV and a SQL table agree on what a filter means; the
/// operators own the operation, this owns the comparison.
/// </remarks>
public static class FilterValueComparer
{
    /// <summary>Whether two values are equal.</summary>
    /// <remarks>
    /// Numerics compare by value across types — a row holding int 5 satisfies a filter written as
    /// "5", because the filter came off a wire or a form and carries no type. Strings compare
    /// case-insensitively, matching what SQL Server does under its default collation, so the same
    /// filter does not mean two things depending on the source.
    /// </remarks>
    public static bool AreEqual(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (TryAsDecimal(left, out var l) && TryAsDecimal(right, out var r))
        {
            return l == r;
        }

        return string.Equals(AsText(left), AsText(right), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Orders two values, returning the usual negative, zero or positive.</summary>
    /// <remarks>
    /// A null on either side yields 0, so an ordering comparison against a missing value never
    /// matches rather than throwing. Anything not numeric and not a date falls back to an ordinal
    /// string comparison.
    /// </remarks>
    public static int Compare(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return 0;
        }

        if (TryAsDecimal(left, out var l) && TryAsDecimal(right, out var r))
        {
            return l.CompareTo(r);
        }

        if (TryAsDateTimeOffset(left, out var ld) && TryAsDateTimeOffset(right, out var rd))
        {
            return ld.CompareTo(rd);
        }

        return string.Compare(AsText(left), AsText(right), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether <paramref name="left"/> appears in <paramref name="right"/>.</summary>
    /// <remarks>
    /// The right side of an In is a collection when it came from code and a comma-separated string
    /// when it came off a wire. Both are accepted because both arrive.
    /// </remarks>
    public static bool IsIn(object? left, object? right)
    {
        if (right is null)
        {
            return false;
        }

        if (right is string text)
        {
            return text.Split(',').Any(part => AreEqual(left, part.Trim()));
        }

        if (right is IEnumerable items)
        {
            return items.Cast<object?>().Any(item => AreEqual(left, item));
        }

        return AreEqual(left, right);
    }

    /// <summary>The value as text, invariant, never null.</summary>
    public static string AsText(object? value) =>
        value switch
        {
            null => string.Empty,
            string s => s,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    private static bool TryAsDecimal(object value, out decimal result)
    {
        switch (value)
        {
            case decimal d: result = d; return true;
            case int or long or short or byte or sbyte or uint or ulong or ushort:
                result = Convert.ToDecimal(value, CultureInfo.InvariantCulture); return true;
            case double or float:
                result = Convert.ToDecimal(value, CultureInfo.InvariantCulture); return true;
            case string s:
                return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            default: result = 0; return false;
        }
    }

    private static bool TryAsDateTimeOffset(object value, out DateTimeOffset result)
    {
        switch (value)
        {
            case DateTimeOffset dto: result = dto; return true;
            case DateTime dt: result = new DateTimeOffset(dt); return true;
            case DateOnly d: result = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue)); return true;
            case string s:
                return DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            default: result = default; return false;
        }
    }
}
