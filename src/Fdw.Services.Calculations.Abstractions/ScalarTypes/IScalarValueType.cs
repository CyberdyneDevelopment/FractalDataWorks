using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Represents a scalar value type supported as a calculation input.
/// </summary>
public interface IScalarValueType : ITypeOption<int, ScalarValueTypeBase>
{
    /// <summary>
    /// Says whether a text is a value of this type, so a calculation that holds the text as an input can be refused when it is saved
    /// and not when it runs.
    /// </summary>
    /// <param name="serializedValue">The value, written as text.</param>
    /// <returns>True when the text is a value of this type.</returns>
    bool IsValidText(string serializedValue);
}
