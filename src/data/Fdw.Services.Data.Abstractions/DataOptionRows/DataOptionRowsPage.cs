using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>One page of a container's option rows (FDW-5664).</summary>
public sealed class DataOptionRowsPage
{
    /// <summary>Gets the rows, in column then value order.</summary>
    public required IReadOnlyList<DataOptionRow> Items { get; init; }

    /// <summary>Gets whether more rows follow this page.</summary>
    public required bool HasMore { get; init; }
}
