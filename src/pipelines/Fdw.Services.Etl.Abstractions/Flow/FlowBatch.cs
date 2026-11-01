using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The unit of rows that moves between components: a small, ordered list of rows (FDW-1386).
/// </summary>
/// <remarks>
/// Rows move in batches so that a component pays for a channel write once per batch, not per row, and so that back-pressure acts
/// on a bounded number of batches. A batch is read-only to its receiver: a component that changes a row writes a new row.
/// </remarks>
public sealed class FlowBatch
{
    /// <summary>Initializes a new instance of the <see cref="FlowBatch"/> class.</summary>
    /// <param name="rows">The rows, in order; the batch keeps this list.</param>
    public FlowBatch(IReadOnlyList<IDictionary<string, object?>> rows)
    {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <summary>Gets the rows of the batch, in order.</summary>
    public IReadOnlyList<IDictionary<string, object?>> Rows { get; }

    /// <summary>Gets the number of rows in the batch.</summary>
    public int Count => Rows.Count;

    /// <summary>
    /// Makes an independent copy: each row is a new dictionary with the same keys and values, so a receiver that edits a row
    /// in place does not change what another receiver of the same output sees.
    /// </summary>
    /// <returns>The copy.</returns>
    public FlowBatch Copy() =>
        new([.. Rows.Select(row => (IDictionary<string, object?>)new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase))]);
}
