using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>The types a literal column's stated value can have: Text, Number or Flag.</summary>
/// <remarks>
/// A value no type holds, and a name no type has, both answer the NotFound sentinel: a caller compares against it and
/// refuses with a coded failure, it never reads through it.
/// </remarks>
[TypeCollection(typeof(LiteralTypeBase), typeof(ILiteralType), typeof(LiteralTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class LiteralTypes : TypeCollectionBase<LiteralTypeBase, ILiteralType>
{
    /// <summary>Finds the type that holds a stated value.</summary>
    /// <param name="value">The stated value.</param>
    /// <returns>The type, or <c>NotFound</c> when none holds the value.</returns>
    public static ILiteralType OfValue(object? value)
    {
        foreach (var type in All())
        {
            if (type.Holds(value))
            {
                return type;
            }
        }

        return NotFound;
    }
}
