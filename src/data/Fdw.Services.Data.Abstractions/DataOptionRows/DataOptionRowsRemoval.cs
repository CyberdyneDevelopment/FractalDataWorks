using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>What <see cref="IDataOptionRows.Remove"/> is asked (FDW-5664).</summary>
public sealed class DataOptionRowsRemoval
{
    /// <summary>Gets the container the rows belong to.</summary>
    public required DataOptionRowsContainer Target { get; init; }

    /// <summary>Gets the ids of the rows to remove, at least one.</summary>
    public required IReadOnlyList<Guid> Ids { get; init; }
}
