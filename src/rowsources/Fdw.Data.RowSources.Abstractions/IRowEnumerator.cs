using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Streaming enumerator that yields rows with per-row Result pattern support.
/// Enables processing millions of rows without loading all into memory.
/// </summary>
/// <remarks>
/// Key features:
/// - IAsyncEnumerable for backpressure via CancellationToken
/// - Per-row IGenericResult for error handling without stopping iteration
/// - Statistics tracking (RowsRead, RowErrors)
/// - Works with any IRowMapper implementation
///
/// Protocol implementations (REST, GraphQL, OData) implement this interface
/// to provide streaming with pagination handling.
/// </remarks>
public interface IRowEnumerator : IAsyncDisposable
{
    /// <summary>
    /// Enumerates rows from the source, yielding each as a Result.
    /// </summary>
    /// <param name="mapper">The row mapper to use for conversion.</param>
    /// <param name="cancellationToken">Cancellation token for aborting iteration.</param>
    /// <returns>An async enumerable of row results.</returns>
    /// <remarks>
    /// Successful rows return Success with the dictionary.
    /// Failed rows return Failure with error details but don't stop iteration.
    /// Use RowErrors property after enumeration to check total failures.
    /// </remarks>
    IAsyncEnumerable<IGenericResult<IDictionary<string, object?>>> EnumerateRows(
        IRowMapper mapper,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of rows read (including errors).
    /// </summary>
    long RowsRead { get; }

    /// <summary>
    /// Gets the total number of row-level errors encountered.
    /// </summary>
    long RowErrors { get; }
}
