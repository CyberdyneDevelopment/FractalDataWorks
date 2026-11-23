using Fdw.Collections;

namespace Fdw.Data;

/// <summary>One kind of value an OData filter can carry, and how it is written as an OData literal.</summary>
public interface IODataLiteralKind : ITypeOption<int, ODataLiteralKindBase>
{
    /// <summary>Tells whether a value is of this kind.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Writes a value of this kind as an OData literal.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The literal, in the invariant culture.</returns>
    string Literal(object value);
}
