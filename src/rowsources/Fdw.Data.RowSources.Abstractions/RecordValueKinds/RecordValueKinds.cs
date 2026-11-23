using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>The kinds of value a record can hold, each with the one canonical text form every record writer shares.</summary>
/// <remarks>A value no kind holds answers the NotFound sentinel; the caller refuses it, never writes its type name.</remarks>
[TypeCollection(typeof(RecordValueKindBase), typeof(IRecordValueKind), typeof(RecordValueKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class RecordValueKinds : TypeCollectionBase<RecordValueKindBase, IRecordValueKind>
{
    /// <summary>Finds the kind that holds a record value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The kind, or <c>NotFound</c> when none holds the value.</returns>
    public static IRecordValueKind OfValue(object value)
    {
        foreach (var kind in All())
        {
            if (kind.Holds(value))
            {
                return kind;
            }
        }

        return NotFound;
    }
}
