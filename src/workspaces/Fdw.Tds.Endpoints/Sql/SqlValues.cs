using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Fdw.Tds.Endpoints.Sql;

/// <summary>
/// How SQL Server compares, converts and prints values, applied to the .NET values this endpoint holds.
/// </summary>
/// <remarks>
/// <para>
/// Text compares the way the server's collation does: case-insensitive, and blind to trailing spaces.
/// Mixed types compare the way SQL Server's type precedence converts them: text against a number becomes
/// a number, text against a date a date, text against a GUID a GUID. A conversion that fails is the
/// caller's to refuse with error 245; it is never compared as something else.
/// </para>
/// </remarks>
public static class SqlValues
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Gets a value indicating whether a value is a number (bit included, as SQL Server treats it).</summary>
    /// <param name="value">The value.</param>
    /// <returns>True for an integer, decimal, floating-point or bit value.</returns>
    public static bool IsNumber(object? value)
        => value is int or long or short or byte or decimal or double or float or bool;

    /// <summary>Compares two values that are not NULL.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <param name="order">Negative, zero or positive, when they compare.</param>
    /// <returns>True when the two can be compared; false when one does not convert to the other's type.</returns>
    public static bool TryCompare(object left, object right, out int order)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (IsNumber(left) || IsNumber(right))
            return TryCompareNumbers(left, right, out order);

        if (left is string a && right is string b)
        {
            order = string.Compare(a.TrimEnd(' '), b.TrimEnd(' '), StringComparison.OrdinalIgnoreCase);
            return true;
        }

        return TryCompareConverted(left, right, out order);
    }

    /// <summary>Converts a value to a number when SQL Server would.</summary>
    /// <param name="value">The value.</param>
    /// <param name="number">The number.</param>
    /// <returns>True when the value is or converts to a number.</returns>
    public static bool TryNumber(object value, out decimal number)
    {
        if (value is string text)
            return decimal.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);

        if (value is double or float)
            return TryFromFloating(Convert.ToDouble(value, CultureInfo.InvariantCulture), out number);

        if (value is bool flag)
        {
            number = flag ? 1m : 0m;
            return true;
        }

        var exact = value is int or long or short or byte or decimal;
        number = exact ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) : 0m;
        return exact;
    }

    /// <summary>Writes a value as SQL Server's CAST to nvarchar writes it.</summary>
    /// <param name="value">The value; not NULL.</param>
    /// <returns>The text.</returns>
    public static string ToText(object value) => value switch
    {
        string text => text,
        bool flag => BitText(flag),
        byte[] bytes => "0x" + Convert.ToHexString(bytes),
        Guid id => id.ToString("D", CultureInfo.InvariantCulture).ToUpperInvariant(),
        IFormattable formattable => formattable.ToString(FormatFor(value), CultureInfo.InvariantCulture),
        _ => $"{value}",
    };

    /// <summary>Matches text against a LIKE pattern the way SQL Server does, ignoring case.</summary>
    /// <param name="text">The text.</param>
    /// <param name="pattern">The pattern: % any run, _ one character, [...] a set.</param>
    /// <param name="escape">The escape character, when the pattern declares one.</param>
    /// <returns>True when the text matches.</returns>
    public static bool Like(string text, string pattern, char? escape)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);
        return Regex.IsMatch(text.TrimEnd(' '), LikeToRegex(pattern.TrimEnd(' '), escape),
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, PatternTimeout);
    }

    private static bool TryCompareNumbers(object left, object right, out int order)
    {
        if (left is double or float || right is double or float)
        {
            var ok = TryDouble(left, out var x) & TryDouble(right, out var y);
            order = ok ? x.CompareTo(y) : 0;
            return ok;
        }

        var exact = TryNumber(left, out var a) & TryNumber(right, out var b);
        order = exact ? a.CompareTo(b) : 0;
        return exact;
    }

    private static string BitText(bool flag) => flag ? "1" : "0";

    // The format SQL Server's CAST to nvarchar uses for a date or time; the type's own for anything else.
    private static string? FormatFor(object value) => value switch
    {
        DateTime => "yyyy-MM-dd HH:mm:ss.fffffff",
        DateTimeOffset => "yyyy-MM-dd HH:mm:ss.fffffff zzz",
        TimeSpan => @"hh\:mm\:ss\.fffffff",
        _ => null,
    };

    private static bool TryFromFloating(double d, out decimal number)
    {
        var fits = !double.IsNaN(d) && !double.IsInfinity(d) && Math.Abs(d) < 7.9e28;
        number = fits ? (decimal)d : 0m;
        return fits;
    }

    private static bool TryDouble(object value, out double number)
    {
        if (value is double d)
        {
            number = d;
            return true;
        }

        if (value is float f)
        {
            number = f;
            return true;
        }

        var ok = TryNumber(value, out var exact);
        number = ok ? (double)exact : 0d;
        return ok;
    }

    // Text against a date, a GUID or a time converts to that type; two values of one type compare as it.
    private static bool TryCompareConverted(object left, object right, out int order)
    {
        if (TryAlike(left, right, out var a, out var b) && a is IComparable comparable && a.GetType() == b.GetType())
        {
            order = comparable.CompareTo(b);
            return true;
        }

        order = 0;
        return false;
    }

    private static bool TryAlike(object left, object right, out object a, out object b)
    {
        a = left;
        b = right;
        if (left.GetType() == right.GetType())
            return true;

        if (left is string text)
            return TryAs(text, right.GetType(), out a);

        return right is string other && TryAs(other, left.GetType(), out b);
    }

    private static bool TryAs(string text, Type type, out object converted)
    {
        var trimmed = text.Trim();
        converted = text;
        if (type == typeof(DateTime) && DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment))
            converted = moment;
        else if (type == typeof(DateTimeOffset) && DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var offset))
            converted = offset;
        else if (type == typeof(Guid) && Guid.TryParse(trimmed, out var id))
            converted = id;
        else if (type == typeof(TimeSpan) && TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out var span))
            converted = span;

        return !ReferenceEquals(converted, text);
    }

    private static string LikeToRegex(string pattern, char? escape)
    {
        var regex = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
            i = AppendLikeToken(regex, pattern, i, escape);

        return regex.Append('$').ToString();
    }

    // Appends the regex for the pattern token at `i` and answers the index of its last character.
    private static int AppendLikeToken(StringBuilder regex, string pattern, int i, char? escape)
    {
        var c = pattern[i];
        if (escape is { } e && c == e && i + 1 < pattern.Length)
        {
            regex.Append(Regex.Escape(pattern[i + 1].ToString()));
            return i + 1;
        }

        if (c == '%')
            regex.Append(".*");
        else if (c == '_')
            regex.Append('.');
        else if (c == '[' && pattern.IndexOf(']', i + 1) is var close and > 0)
            return AppendSet(regex, pattern, i, close);
        else
            regex.Append(Regex.Escape(c.ToString()));

        return i;
    }

    private static int AppendSet(StringBuilder regex, string pattern, int open, int close)
    {
        var body = pattern.Substring(open + 1, close - open - 1);
        var negated = body.StartsWith('^');
        regex.Append('[');
        if (negated)
            regex.Append('^');

        regex.Append(Regex.Escape(negated ? body[1..] : body));
        regex.Append(']');
        return close;
    }
}
