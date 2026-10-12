using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Schema;

/// <summary>
/// What a column's data is: an identifier, a category, a date, a measure, metadata and so on (FDW-988).
/// </summary>
/// <remarks>
/// <para>
/// A datum type is separate from a column's data type (int, varchar) and from its key type. Two int
/// columns can be a measure and a date encoded as yyyymmdd; the data type cannot tell them apart, the
/// datum type does.
/// </para>
/// <para>
/// Everything a caller decides from a column's meaning lives on its datum type: which filters and
/// comparisons it takes, whether it orders, groups or aggregates, whether it is ever displayed, and
/// whether a data command returns it when nobody asked for it. No caller switches on a datum type's name.
/// </para>
/// </remarks>
public interface IDatumType : ITypeOption<int, DatumTypeBase>
{
    /// <summary>Gets a human-readable description of what data of this type is.</summary>
    string Description { get; }

    /// <summary>Gets whether a column of this type identifies its record (the columns a schema's key is built from).</summary>
    bool IsKeyRole { get; }

    /// <summary>Gets whether a column of this type is worth an index.</summary>
    bool IsIndexable { get; }

    /// <summary>Gets whether a column of this type can be summed, averaged and the like.</summary>
    bool IsAggregatable { get; }

    /// <summary>Gets whether a result can be ordered by a column of this type.</summary>
    bool IsOrderable { get; }

    /// <summary>Gets whether a result can be grouped by a column of this type.</summary>
    bool IsGroupable { get; }

    /// <summary>
    /// Gets whether a column of this type is a point in time that describes the record -- when an event
    /// happened, when someone joined, the hour something ran -- as opposed to an edge of a validity period.
    /// </summary>
    bool IsPointInTime { get; }

    /// <summary>
    /// Gets whether a column of this type is where a status or dimension record starts to hold: the lower
    /// edge of its validity period. A record holds at a moment t when its lower boundary is at or before t
    /// and its upper boundary is after t.
    /// </summary>
    bool IsLowerDateBoundary { get; }

    /// <summary>
    /// Gets whether a column of this type is where a status or dimension record stops holding: the upper
    /// edge of its validity period.
    /// </summary>
    bool IsUpperDateBoundary { get; }

    /// <summary>Gets whether a column of this type may be shown to a person at all.</summary>
    bool IsDisplayable { get; }

    /// <summary>
    /// Gets the names of the <c>FilterOperators</c> a filter on a column of this type may use. The column's
    /// data type narrows this further: a range comparison on a column that cannot be ordered is still refused.
    /// </summary>
    IReadOnlyList<string> AllowedOperators { get; }

    /// <summary>Gets the names of the <see cref="AxisTypes"/> a field of this type can sit on as it stands.</summary>
    /// <remarks>
    /// A category, a time, a place, or a number a chart plots. A type that can be counted but not plotted raw
    /// (an attribute, say) lists only the first; it reaches the value of a chart through <see cref="Aggregates"/>.
    /// </remarks>
    IReadOnlyList<string> Axes { get; }

    /// <summary>Gets the names of the aggregates over a field of this type: <see cref="DatumAggregates"/>.</summary>
    IReadOnlyList<string> Aggregates { get; }

    /// <summary>Gets whether a field of this type can sit on an axis as it stands.</summary>
    /// <param name="axis">An <see cref="AxisTypes"/> option name.</param>
    /// <returns>True when <see cref="Axes"/> lists it, compared ignoring case.</returns>
    bool AcceptsAxis(string axis);

    /// <summary>Gets whether an aggregate over a field of this type is allowed.</summary>
    /// <param name="aggregate">A <see cref="DatumAggregates"/> name.</param>
    /// <returns>True when <see cref="Aggregates"/> lists it, compared ignoring case.</returns>
    bool AllowsAggregate(string aggregate);

    /// <summary>
    /// Gets whether a query returns a column of this type when it names no columns of its own and asks for
    /// nothing extra: true for the detail types, false for Metadata, Descriptive and DbKey, each returned only
    /// when the data command includes it.
    /// </summary>
    bool IsDefaultResult { get; }

    /// <summary>
    /// Says whether a data command that names no columns returns a column of this type, given what the
    /// command includes (QueryCommand.IncludeMetadata, QueryCommand.IncludeDescriptions, QueryCommand.IncludeKeys).
    /// </summary>
    /// <param name="includeMetadata">Whether the query asked for metadata columns.</param>
    /// <param name="includeDescriptions">Whether the query asked for descriptive columns.</param>
    /// <param name="includeKeys">
    /// Whether the query asked for physical key columns. Only server-side reads set it: a configuration
    /// read that joins or re-versions a row needs its RowId. A key is never shown to a person either way.
    /// </param>
    /// <returns>True to select the column.</returns>
    bool IsReturned(bool includeMetadata, bool includeDescriptions, bool includeKeys);
}
