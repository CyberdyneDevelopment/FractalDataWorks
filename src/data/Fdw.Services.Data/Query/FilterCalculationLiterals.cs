using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// Moves values between a filter's JSON and a calculation: the filter's input values become what the calculation reads, and what it
/// yields becomes the literal the filter would have held.
/// </summary>
internal static class FilterCalculationLiterals
{
    // The types of field a calculation's result may be compared with, by the type the calculation yields. One entry per type, so a
    // new kind of result is one entry and no method grows with it.
    private static readonly Dictionary<Type, HashSet<Type>> Accepted = new()
    {
        [typeof(decimal)] = [typeof(byte), typeof(short), typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(float)],
        [typeof(DateTimeOffset)] = [typeof(DateTime), typeof(DateTimeOffset)],
    };

    private static readonly Dictionary<Type, string> Words = new()
    {
        [typeof(decimal)] = "a number",
        [typeof(DateTimeOffset)] = "a date-time",
    };

    /// <summary>Reads the value a filter supplies for a calculation's input.</summary>
    /// <param name="value">The value as the filter states it.</param>
    /// <param name="input">The input's name, for a refusal.</param>
    /// <param name="calculation">The calculation's name, for a refusal.</param>
    /// <param name="refusals">Builds the refusal.</param>
    /// <returns>A decimal for a number, a bool for a flag, or the refusal.</returns>
    public static IGenericResult<object?> Input(JsonElement value, string input, string calculation, IQueryRefusals refusals)
    {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
        switch (value.ValueKind)
        {
            case JsonValueKind.Number when value.TryGetDecimal(out var number):
                return GenericResult<object?>.Success(number);
            case JsonValueKind.True:
                return GenericResult<object?>.Success(true);
            case JsonValueKind.False:
                return GenericResult<object?>.Success(false);
            default:
                return refusals.Invalid<object?>($"input '{input}' of calculation '{calculation}' must be a number or true or false");
        }
#pragma warning restore FDW018
    }

    /// <summary>Checks a calculation's result against the type of the field it is compared with.</summary>
    /// <param name="value">What the calculation yielded.</param>
    /// <param name="calculation">The calculation's name, for a refusal.</param>
    /// <param name="field">The field the condition is on.</param>
    /// <param name="refusals">Builds the refusal.</param>
    /// <returns>Success, or the refusal that names the field, the calculation and both types.</returns>
    public static IGenericResult Check(FilterCalculationValue value, string calculation, QueryableField field, IQueryRefusals refusals)
    {
        if (field.ValueType is not { } fieldType)
        {
            return refusals.Invalid<bool>($"'{field.Name}' declares no type, so the result of calculation '{calculation}' cannot be checked against it");
        }

        if (!Accepted.TryGetValue(value.ValueType, out var fits))
        {
            return refusals.Invalid<bool>($"calculation '{calculation}' yields a {value.ValueType.Name}, which a filter cannot compare");
        }

        return fits.Contains(fieldType)
            ? GenericResult.Success()
            : refusals.Invalid<bool>($"calculation '{calculation}' yields {Words[value.ValueType]}, and '{field.Name}' holds a {fieldType.Name}");
    }

    /// <summary>Writes a calculation's result as the literal a filter holds.</summary>
    /// <param name="value">What the calculation yielded, already checked against the field.</param>
    /// <returns>A JSON string in ISO 8601 for a date-time, a JSON number for a number.</returns>
    public static JsonElement Literal(FilterCalculationValue value)
        => value.Value is DateTimeOffset moment
            ? JsonSerializer.SerializeToElement(moment.ToString("O", CultureInfo.InvariantCulture))
            : JsonSerializer.SerializeToElement((decimal)value.Value);
}
