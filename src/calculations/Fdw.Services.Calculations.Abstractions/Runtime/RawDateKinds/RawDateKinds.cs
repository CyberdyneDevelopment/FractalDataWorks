using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>The kinds of value a record can hold in a field read as a date: a date, a date-time, or nothing.</summary>
/// <remarks>A value no kind holds answers the NotFound sentinel; the reader fails the run with the field-not-a-date code.</remarks>
[TypeCollection(typeof(RawDateKindBase), typeof(IRawDateKind), typeof(RawDateKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class RawDateKinds : TypeCollectionBase<RawDateKindBase, IRawDateKind>
{
    /// <summary>Finds the kind that holds a record's field value.</summary>
    /// <param name="raw">The value; null when the field holds none.</param>
    /// <returns>The kind, or <c>NotFound</c> when none holds the value.</returns>
    public static IRawDateKind OfValue(object? raw)
    {
        foreach (var kind in All())
        {
            if (kind.Holds(raw))
            {
                return kind;
            }
        }

        return NotFound;
    }
}
