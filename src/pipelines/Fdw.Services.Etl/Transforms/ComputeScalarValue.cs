using Fdw.Services.Calculations.Abstractions.Sets;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Reads a single value a Compute stage hands to an input: written as text with its type, and refused when it is not what its
/// type says, never converted to something else.
/// </summary>
public static class ComputeScalarValue
{
    /// <summary>Reads one value.</summary>
    /// <param name="text">The value as text.</param>
    /// <param name="valueType">Number, Flag or Text.</param>
    /// <param name="value">The value as its type, or null when it could not be read.</param>
    /// <returns>True when the text is a value of that type.</returns>
    public static bool TryParse(string text, string valueType, out object? value)
    {
        value = null;
        var type = LiteralTypes.ByName(valueType);
        if (ReferenceEquals(type, LiteralTypes.NotFound))
        {
            return false;
        }

        value = type.Read(text);
        return value is not null;
    }
}
