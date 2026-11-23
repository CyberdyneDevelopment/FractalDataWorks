using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>The shapes a row can have: a dictionary of column values, a read-only one, or an entity with a property per column.</summary>
/// <remarks>A row no kind holds answers the NotFound sentinel; the three kinds hold every row.</remarks>
[TypeCollection(typeof(RowKindBase), typeof(IRowKind), typeof(RowKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class RowKinds : TypeCollectionBase<RowKindBase, IRowKind>
{
    /// <summary>Finds the shape of a row.</summary>
    /// <param name="row">The row, which is not null.</param>
    /// <returns>The kind.</returns>
    /// <exception cref="InvalidOperationException">No kind holds the row, which the three kinds make impossible.</exception>
    public static IRowKind OfRow(object row)
    {
        foreach (var kind in All())
        {
            if (kind.Holds(row))
            {
                return kind;
            }
        }

        throw new InvalidOperationException($"A row of type '{row.GetType().FullName}' has no row kind.");
    }
}
