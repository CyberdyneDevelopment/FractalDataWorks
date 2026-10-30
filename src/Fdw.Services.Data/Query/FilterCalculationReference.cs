using System.Collections.Generic;
using System.Text.Json;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// A filter value that names a calculation: <c>{"calculation": "Yesterday"}</c>, or with values for the calculation's inputs
/// <c>{"calculation": "Seconds ago", "inputs": {"seconds": 33}}</c>. It stands wherever a literal value does: a condition's
/// value, the ends of a date range, the bounds of a Between.
/// </summary>
/// <remarks>
/// The saved filter keeps the name and the inputs, so it is always evaluated again when the query runs.
/// </remarks>
public sealed class FilterCalculationReference
{
    private const string CalculationMember = "calculation";
    private const string InputsMember = "inputs";

    private FilterCalculationReference(string name, IReadOnlyDictionary<string, JsonElement> inputs)
    {
        Name = name;
        Inputs = inputs;
    }

    /// <summary>Gets the calculation's name.</summary>
    public string Name { get; }

    /// <summary>Gets the values the filter supplies for the calculation's inputs, by input name.</summary>
    public IReadOnlyDictionary<string, JsonElement> Inputs { get; }

    /// <summary>Tells whether a filter value is a calculation reference rather than a literal.</summary>
    /// <param name="value">The value as the filter states it.</param>
    /// <returns>True when the value is an object that has a <c>calculation</c> member.</returns>
    public static bool IsReference(JsonElement value)
    {
        if (!IsObject(value))
        {
            return false;
        }

        foreach (var property in value.EnumerateObject())
        {
            if (string.Equals(property.Name, CalculationMember, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Tells whether a filter value is a reference or an object with a reference as one of its members, as a Between is.</summary>
    /// <param name="value">The value as the filter states it, or null when the member is absent.</param>
    /// <returns>True when the value still has a reference in it.</returns>
    public static bool HoldsReference(JsonElement? value)
    {
        if (value is not { } given)
        {
            return false;
        }

        if (IsReference(given))
        {
            return true;
        }

        if (!IsObject(given))
        {
            return false;
        }

        foreach (var property in given.EnumerateObject())
        {
            if (IsReference(property.Value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reads a reference, refusing one whose shape is wrong.</summary>
    /// <param name="value">The value, for which <see cref="IsReference"/> is true.</param>
    /// <param name="field">The field the condition is on, for a refusal.</param>
    /// <param name="refusals">Builds the refusal.</param>
    /// <returns>The reference, or the refusal naming the field and what is wrong.</returns>
    public static IGenericResult<FilterCalculationReference> Read(JsonElement value, string field, IQueryRefusals refusals)
    {
        string? name = null;
        var inputs = new Dictionary<string, JsonElement>(System.StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (string.Equals(property.Name, CalculationMember, System.StringComparison.OrdinalIgnoreCase))
            {
                name = IsText(property.Value) ? property.Value.GetString() : null;
            }
            else if (string.Equals(property.Name, InputsMember, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!IsObject(property.Value))
                {
                    return refusals.Invalid<FilterCalculationReference>(
                        $"the calculation reference on '{field}' needs 'inputs' to be an object of input values");
                }

                foreach (var input in property.Value.EnumerateObject())
                {
                    inputs[input.Name] = input.Value;
                }
            }
            else
            {
                return refusals.Invalid<FilterCalculationReference>(
                    $"the calculation reference on '{field}' has an unknown member '{property.Name}'; it takes '{CalculationMember}' and '{InputsMember}'");
            }
        }

        return string.IsNullOrWhiteSpace(name)
            ? refusals.Invalid<FilterCalculationReference>($"the calculation reference on '{field}' needs the calculation's name as text")
            : GenericResult<FilterCalculationReference>.Success(new FilterCalculationReference(name!, inputs));
    }

    private static bool IsText(JsonElement element)
    {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
        return element.ValueKind == JsonValueKind.String;
#pragma warning restore FDW018
    }

    private static bool IsObject(JsonElement element)
    {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
        return element.ValueKind == JsonValueKind.Object;
#pragma warning restore FDW018
    }
}
