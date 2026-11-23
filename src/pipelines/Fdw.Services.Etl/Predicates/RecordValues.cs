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

        return value is not null && RecordValueShapes.Of(value).IsTrue(value);
    }

    private static bool TryGetMoment(object value, out DateTimeOffset moment) => RecordValueShapes.Of(value).TryMoment(value, out moment);

    private static bool TryGetDecimal(object value, out decimal number) => RecordValueShapes.Of(value).TryDecimal(value, out number);
}
