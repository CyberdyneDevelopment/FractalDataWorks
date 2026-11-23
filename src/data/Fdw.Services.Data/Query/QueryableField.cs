using System;
using System.Collections.Generic;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A field a query may name, the column it reaches and the type its values have.</summary>
/// <param name="Name">The name a request uses; matched ignoring case.</param>
/// <param name="Column">The property the query is built against, in the case the store knows it by.</param>
/// <param name="ValueType">
/// The type the field's values have, as the resource or the data set declares it. A filter value is parsed to
/// this before any command is built, so a value that is not of the type is refused naming the field instead
/// of reaching the store. <see langword="null"/> says the field declares no type -- a data set field whose
/// type name is not a known one -- and its values are passed as the JSON gave them.
/// </param>
/// <param name="IsCalculated">
/// Whether the field is computed after the rows are read, so the store can neither filter nor order on it.
/// </param>
/// <param name="IsFilterable">
/// Whether a caller may filter on the field. Read only by <see cref="QueryTranslator.RequireFilterable"/>, which a route
/// runs on the caller's own filter; a filter the route adds itself (a view's) is never held to it.
/// </param>
/// <param name="FilterOperators">
/// The operators a caller's filter may use on the field, by name; a date range (<c>startDate</c> and <c>endDate</c>) counts
/// as <c>Between</c>. Read only by <see cref="QueryTranslator.RequireFilterable"/>. <see langword="null"/> says the surface
/// declares none (a configuration resource or a table that has no datum types), so no operator is checked; an empty list
/// says the field takes none.
/// </param>
/// <param name="NoOperatorsReason">
/// Why <paramref name="FilterOperators"/> is empty, in the words a refusal uses; when it is null a refusal says only that
/// the field takes no filter operator.
/// </param>
public sealed record QueryableField(
    string Name,
    string Column,
    Type? ValueType,
    bool IsCalculated = false,
    bool IsFilterable = true,
    IReadOnlyList<string>? FilterOperators = null,
    string? NoOperatorsReason = null)
{
    /// <summary>
    /// Reads a filled field's value off the record a page holds; <see langword="null"/> for a field the store holds in <see cref="Column"/>.
    /// </summary>
    /// <remarks>
    /// A filled field is one the provider puts on the record after the rows are read, from another domain's rows (a description kept in the descriptions
    /// table in the caller's language, FDW-6208). The record's mapper does not reach it, the list's table holds no column of it, and a view cannot join it.
    /// </remarks>
    public Func<object, object?>? Read { get; init; }

    /// <summary>Gets whether the field is filled after the read rather than held in a column (FDW-6273).</summary>
    public bool IsFilled => Read is not null;

    /// <summary>Declares a field the provider fills after the read: asked for, it is read off the record with <paramref name="read"/>; it cannot be filtered or sorted on.</summary>
    /// <typeparam name="TRecord">The record a page of the resource holds.</typeparam>
    /// <param name="name">The name a request uses; matched ignoring case.</param>
    /// <param name="member">The record's member the provider fills, named for a refusal and a reader; no column is reached by it.</param>
    /// <param name="valueType">The type the field's values have.</param>
    /// <param name="read">Reads the filled value off the record.</param>
    /// <returns>The declaration.</returns>
    public static QueryableField Filled<TRecord>(string name, string member, Type valueType, Func<TRecord, object?> read)
        => new(name, member, valueType, IsFilterable: false) { Read = record => read((TRecord)record) };
}
