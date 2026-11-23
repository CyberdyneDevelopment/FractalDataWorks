using System;
using System.Linq;
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
    /// <summary>Reads the value a filter supplies for a calculation's input.</summary>
    /// <param name="value">The value as the filter states it.</param>
    /// <param name="input">The input's name, for a refusal.</param>
    /// <param name="calculation">The calculation's name, for a refusal.</param>
    /// <param name="refusals">Builds the refusal.</param>
    /// <returns>A decimal for a number, a bool for a flag, a DateOnly for a text of the form yyyy-MM-dd, or the refusal.</returns>
    public static IGenericResult<object?> Input(JsonElement value, string input, string calculation, IQueryRefusals refusals)
    {
        var kind = JsonInputKinds.ByValueKind(value.ValueKind);
        var read = ReferenceEquals(kind, JsonInputKinds.NotFound) ? null : kind.Read(value);
        return read is null
            ? refusals.Invalid<object?>($"input '{input}' of calculation '{calculation}' must be a number, true or false, or a date written yyyy-MM-dd")
            : GenericResult<object?>.Success(read);
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

        var kind = FilterCalculationResultKinds.ByValueType(value.ValueType);
        if (ReferenceEquals(kind, FilterCalculationResultKinds.NotFound))
        {
            return refusals.Invalid<bool>($"calculation '{calculation}' yields a {value.ValueType.Name}, which a filter cannot compare");
        }

        return kind.FieldTypes.Contains(fieldType)
            ? GenericResult.Success()
            : refusals.Invalid<bool>($"calculation '{calculation}' yields {kind.Word}, and '{field.Name}' holds a {fieldType.Name}");
    }

    /// <summary>Writes a calculation's result as the literal a filter holds.</summary>
    /// <param name="value">What the calculation yielded, already checked against the field.</param>
    /// <returns>A JSON string in ISO 8601 for a date-time, yyyy-MM-dd for a date, a JSON number for a number.</returns>
    /// <exception cref="InvalidOperationException">The result is of a type no kind holds; <see cref="Check"/> refuses that first.</exception>
    public static JsonElement Literal(FilterCalculationValue value)
    {
        var kind = FilterCalculationResultKinds.ByValueType(value.ValueType);
        return ReferenceEquals(kind, FilterCalculationResultKinds.NotFound)
            ? throw new InvalidOperationException($"A calculation result of type '{value.ValueType.Name}' has no filter literal; the check refuses it before one is written.")
            : kind.Literal(value.Value);
    }
}
