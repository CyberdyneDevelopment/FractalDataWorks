using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// The join target's resolved <see cref="IDataContainer"/> together with its decoded rows, returned by a
/// <see cref="JoinedRowsLoader"/>.
/// </summary>
/// <remarks>
/// Why the container travels with the rows: <see cref="RecordQueryEvaluator"/> needs the joined
/// container's DECLARED field schema to validate the join's parent-side column and any
/// parent-qualified filter columns (<see cref="RecordColumnValidator"/>), and to validate the loaded
/// parent rows against that declared schema (<see cref="RecordRowValidator"/>) — the same guarantee
/// already applied to the primary rows. A transport's loader resolves the container as part of loading
/// its rows anyway (see <c>FileSystemConnection.LoadJoinedRows</c>), so handing it back costs nothing.
/// </remarks>
public sealed class JoinedRowsResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinedRowsResult"/> class.
    /// </summary>
    /// <param name="container">The resolved join target container.</param>
    /// <param name="rows">The join target's decoded rows.</param>
    public JoinedRowsResult(IDataContainer container, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        Container = container;
        Rows = rows;
    }

    /// <summary>
    /// Gets the resolved join target container.
    /// </summary>
    public IDataContainer Container { get; }

    /// <summary>
    /// Gets the join target's decoded rows.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; }
}
