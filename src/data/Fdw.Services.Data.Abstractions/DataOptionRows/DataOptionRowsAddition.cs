using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>What <see cref="IDataOptionRows.Add"/> is asked (FDW-5664).</summary>
public sealed class DataOptionRowsAddition
{
    /// <summary>Gets the container the rows are for.</summary>
    public required DataOptionRowsContainer Target { get; init; }

    /// <summary>Gets the rows to add, at least one.</summary>
    public required IReadOnlyList<NewDataOptionRow> Rows { get; init; }
}
