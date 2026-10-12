using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>
/// The small static operations compiled calculation code calls: reading a field, unwrapping an empty value, dividing, and
/// comparing. They are public because the code the compiler emits calls them; nothing else should.
/// </summary>
/// <remarks>
/// Why helpers and not inline expressions: each argument is evaluated once, a failure is recorded in one place, and the
/// emitted tree stays small. An empty value is a null; a comparison with an empty operand is false.
/// </remarks>
public static class CalculationRuntime
{
    private static readonly int DivisionByZeroId = CalculationTreeResultCodes.ByName("DivisionByZero").Id;
    private static readonly int EmptyOperandId = CalculationTreeResultCodes.ByName("EmptyOperand").Id;
    private static readonly int RequiredValueEmptyId = CalculationTreeResultCodes.ByName("RequiredValueEmpty").Id;
    private static readonly int FieldNotNumericId = CalculationTreeResultCodes.ByName("FieldNotNumeric").Id;
    private static readonly int FieldNotFlagId = CalculationTreeResultCodes.ByName("FieldNotFlag").Id;

    private static readonly HashSet<Type> IntegerTypes = new()
    {
        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong)
    };

    /// <summary>Reads a field of the record as a number. An absent field, or a value that is not exact, fails; a null or blank value is empty.</summary>
    /// <param name="record">The record.</param>
    /// <param name="field">The field name.</param>
    /// <param name="state">The run state a failure is recorded in.</param>
    /// <param name="node">The index of the reading node.</param>
    /// <returns>The value, or null when it is empty or the read failed.</returns>
    public static decimal? ReadNumber(IReadOnlyDictionary<string, object?> record, string field, CalculationRunState state, int node)
    {
        if (!record.TryGetValue(field, out var raw))
        {
            state.FailWith(FieldNotNumericId, node, $"field '{field}' is not in the record");
            return null;
        }

        return Widen(raw, field, FieldNotNumericId, state, node);
    }

    /// <summary>Reads a field of the record as a true/false flag. Accepts a boolean, or the number 0 or 1.</summary>
    /// <param name="record">The record.</param>
    /// <param name="field">The field name.</param>
    /// <param name="state">The run state a failure is recorded in.</param>
    /// <param name="node">The index of the reading node.</param>
    /// <returns>The flag, or false when the read failed.</returns>
    public static bool ReadFlag(IReadOnlyDictionary<string, object?> record, string field, CalculationRunState state, int node)
    {
        if (!record.TryGetValue(field, out var raw))
        {
            return state.FailFlagWith(FieldNotFlagId, node, $"field '{field}' is not in the record");
        }

        if (raw is bool flag)
        {
            return flag;
        }

        var number = Widen(raw, field, FieldNotFlagId, state, node);
        if (number is null)
        {
            return state.FailFlagWith(EmptyOperandId, node, $"flag field '{field}' is empty");
        }

        if (number == 0m)
        {
            return false;
        }

        return number == 1m || state.FailFlagWith(FieldNotFlagId, node, $"field '{field}' must be true, false, 0 or 1");
    }

    /// <summary>Turns a possibly empty number into a number; an empty one fails with EmptyOperand.</summary>
    /// <param name="value">The value.</param>
    /// <param name="state">The run state a failure is recorded in.</param>
    /// <param name="node">The index of the node that needs the number.</param>
    /// <returns>The number, or zero when it was empty.</returns>
    public static decimal Unwrap(decimal? value, CalculationRunState state, int node) =>
        value.HasValue ? value.GetValueOrDefault() : state.Fail(EmptyOperandId, node);

    /// <summary>Turns a possibly empty number into a number; an empty one fails with RequiredValueEmpty.</summary>
    /// <param name="value">The value.</param>
    /// <param name="state">The run state a failure is recorded in.</param>
    /// <param name="node">The index of the Require node.</param>
    /// <returns>The number, or zero when it was empty.</returns>
    public static decimal Required(decimal? value, CalculationRunState state, int node) =>
        value.HasValue ? value.GetValueOrDefault() : state.Fail(RequiredValueEmptyId, node);

    /// <summary>Divides, failing with DivisionByZero when the divisor is zero.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <param name="state">The run state a failure is recorded in.</param>
    /// <param name="node">The index of the Divide node.</param>
    /// <returns>The quotient, or zero when the divisor was zero.</returns>
    public static decimal Divide(decimal left, decimal right, CalculationRunState state, int node) =>
        right == 0m ? state.Fail(DivisionByZeroId, node) : left / right;

    /// <summary>Tests equality; false when either operand is empty.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when both have a value and the values are equal.</returns>
    public static bool Equal(decimal? left, decimal? right) => left.HasValue && right.HasValue && left.GetValueOrDefault() == right.GetValueOrDefault();

    /// <summary>Tests inequality; false when either operand is empty.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when both have a value and the values differ.</returns>
    public static bool NotEqual(decimal? left, decimal? right) => left.HasValue && right.HasValue && left.GetValueOrDefault() != right.GetValueOrDefault();

    /// <summary>Tests greater than; false when either operand is empty.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when both have a value and the left is greater.</returns>
    public static bool Greater(decimal? left, decimal? right) => left.HasValue && right.HasValue && left.GetValueOrDefault() > right.GetValueOrDefault();

    /// <summary>Tests greater than or equal; false when either operand is empty.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when both have a value and the left is not less.</returns>
    public static bool GreaterOrEqual(decimal? left, decimal? right) => left.HasValue && right.HasValue && left.GetValueOrDefault() >= right.GetValueOrDefault();

    /// <summary>Tests less than; false when either operand is empty.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when both have a value and the left is less.</returns>
    public static bool Less(decimal? left, decimal? right) => left.HasValue && right.HasValue && left.GetValueOrDefault() < right.GetValueOrDefault();

    /// <summary>Tests less than or equal; false when either operand is empty.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when both have a value and the left is not greater.</returns>
    public static bool LessOrEqual(decimal? left, decimal? right) => left.HasValue && right.HasValue && left.GetValueOrDefault() <= right.GetValueOrDefault();

    // Why only decimal and the integer types: a double or float converted to decimal rounds to 15 significant digits, which would
    // silently break reproducibility. A blank text is empty; any other text is refused rather than parsed.
    private static decimal? Widen(object? raw, string field, int code, CalculationRunState state, int node)
    {
        if (raw is null || raw is DBNull)
        {
            return null;
        }

        if (raw is decimal exact)
        {
            return exact;
        }

        if (raw is string text && string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (IntegerTypes.Contains(raw.GetType()))
        {
            return Convert.ToDecimal(raw, CultureInfo.InvariantCulture);
        }

        state.FailWith(code, node, $"field '{field}' holds a {raw.GetType().Name}; only decimal and whole-number values are exact");
        return null;
    }
}
