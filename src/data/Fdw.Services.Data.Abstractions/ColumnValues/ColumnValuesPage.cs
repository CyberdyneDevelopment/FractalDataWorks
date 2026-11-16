using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.ColumnValues;

/// <summary>One page of a column's distinct values (FDW-5666).</summary>
public sealed class ColumnValuesPage
{
    /// <summary>Gets the values on this page, in value order.</summary>
    public required IReadOnlyList<ColumnValue> Items { get; init; }

    /// <summary>Gets whether more values match beyond this page.</summary>
    public required bool HasMore { get; init; }
}
