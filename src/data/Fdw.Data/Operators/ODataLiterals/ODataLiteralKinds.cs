using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>The kinds of value an OData filter can carry, each written as an OData literal.</summary>
/// <remarks>The Other kind holds every value the others do not and writes it as quoted text, so every value has a literal.</remarks>
[TypeCollection(typeof(ODataLiteralKindBase), typeof(IODataLiteralKind), typeof(ODataLiteralKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class ODataLiteralKinds : TypeCollectionBase<ODataLiteralKindBase, IODataLiteralKind>
{
    /// <summary>Writes a value as an OData literal.</summary>
    /// <param name="value">The value; null is written as the literal null.</param>
    /// <returns>The literal.</returns>
    /// <exception cref="InvalidOperationException">No kind holds the value, which the Other kind makes impossible.</exception>
    public static string Format(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        foreach (var kind in All())
        {
            if (kind.Holds(value))
            {
                return kind.Literal(value);
            }
        }

        throw new InvalidOperationException($"A value of type '{value.GetType().FullName}' has no OData literal kind.");
    }
}
