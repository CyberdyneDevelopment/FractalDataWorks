using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>One type a literal column's stated value can have, and how that value is stored as text and read back.</summary>
public interface ILiteralType : ITypeOption<int, LiteralTypeBase>
{
    /// <summary>Tells whether a stated value is of this type.</summary>
    /// <param name="value">The stated value.</param>
    /// <returns>True when this type holds the value.</returns>
    bool Holds(object? value);

    /// <summary>Writes a stated value of this type as the text it is stored as.</summary>
    /// <param name="value">A value this type <see cref="Holds"/>.</param>
    /// <returns>The value's text, in the invariant culture.</returns>
    string Write(object value);

    /// <summary>Reads stored text as a value of this type.</summary>
    /// <param name="text">The stored text.</param>
    /// <returns>The value, or null when the text is not a value of this type.</returns>
    object? Read(string text);
}
