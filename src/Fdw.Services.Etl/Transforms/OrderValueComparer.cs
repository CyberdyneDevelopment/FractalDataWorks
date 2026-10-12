using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Orders the values of one record field in ascending order: null first, numbers by value whatever their type, strings
/// by ordinal comparison, other values of the same type by their own order.
/// </summary>
/// <remarks>
/// Why a recorded problem and not a throw: <see cref="IComparer{T}"/> cannot return a failure, and two values with no
/// common order (a number against text) are bad input, not a bug. The first such pair is kept in
/// <see cref="Problem"/>; the transform reads it after ordering and refuses the step, so a mixed column never produces a
/// quietly wrong order.
/// </remarks>
internal sealed class OrderValueComparer : IComparer<object?>
{
    /// <summary>Gets the types of the first pair of values that have no common order, or null when every pair had one.</summary>
    public (string Left, string Right)? Problem { get; private set; }

    /// <inheritdoc />
    public int Compare(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return CompareNullable(left, right);
        }

        if (IsNumber(left) && IsNumber(right))
        {
            return CompareNumbers(left, right);
        }

        if (left is string leftText && right is string rightText)
        {
            return string.CompareOrdinal(leftText, rightText);
        }

        return CompareSameType(left, right);
    }

    internal static bool IsNumber(object value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static int CompareNumbers(object left, object right)
    {
        if (left is float or double || right is float or double)
        {
            return Convert.ToDouble(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(right, CultureInfo.InvariantCulture));
        }

        return Convert.ToDecimal(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(right, CultureInfo.InvariantCulture));
    }

    private static int CompareNullable(object? left, object? right)
    {
        if (left is null)
        {
            return right is null ? 0 : -1;
        }

        return 1;
    }

    private int CompareSameType(object left, object right)
    {
        if (left.GetType() == right.GetType() && left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        Problem ??= (left.GetType().Name, right.GetType().Name);
        return 0;
    }
}
