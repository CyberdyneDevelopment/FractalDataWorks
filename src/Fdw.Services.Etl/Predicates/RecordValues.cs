using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// How a record's value is compared with the text a condition names. The rules are the ones the Filter stage has always
/// applied: a point in time compares as a point in time, a number as a number, anything else as text ignoring case.
/// </summary>
internal static class RecordValues
{
    public static bool TextEquals(IDictionary<string, object?> record, string fieldName, string expected)
    {
        if (!record.TryGetValue(fieldName, out var actual))
        {
            return false;
        }

        if (actual is null)
        {
            return string.IsNullOrEmpty(expected);
        }

        return string.Equals(actual.ToString(), expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The sign of the record's value against the text, or <see langword="null"/> when the record holds no value there.
    /// </summary>
    public static int? Order(IDictionary<string, object?> record, string fieldName, string text)
    {
        if (!record.TryGetValue(fieldName, out var actual) || actual is null)
        {
            return null;
        }

        if (TryGetMoment(actual, out var actualMoment)
            && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var textMoment))
        {
            return actualMoment.CompareTo(textMoment);
        }

        if (TryGetDecimal(actual, out var actualNumber)
            && decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var textNumber))
        {
            return actualNumber.CompareTo(textNumber);
        }

        return string.Compare(actual.ToString(), text, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTrue(IDictionary<string, object?> record, string fieldName)
    {
        if (!record.TryGetValue(fieldName, out var value))
        {
            return false;
        }

        return value switch
        {
            bool flag => flag,
            string text => bool.TryParse(text, out var parsed) && parsed,
            _ => value is not null
        };
    }

    // A DateTime that states no kind is read as UTC, the same rule the DateTimeOffset parameter type applies to a text
    // without an offset.
    private static bool TryGetMoment(object value, out DateTimeOffset moment)
    {
        switch (value)
        {
            case DateTimeOffset offset:
                moment = offset;
                return true;
            case DateTime time:
                moment = new DateTimeOffset(time.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : time);
                return true;
            default:
                moment = default;
                return false;
        }
    }

    private static bool TryGetDecimal(object value, out decimal number)
    {
        switch (value)
        {
            case decimal exact:
                number = exact;
                return true;
            case double floating:
                number = (decimal)floating;
                return true;
            case float single:
                number = (decimal)single;
                return true;
            case int whole:
                number = whole;
                return true;
            case long wide:
                number = wide;
                return true;
            case short narrow:
                number = narrow;
                return true;
            case byte small:
                number = small;
                return true;
            default:
                return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
        }
    }
}
