using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Asynchronous row source reader for streaming scenarios.
/// Use for network streams, large files, and paginated API responses.
/// </summary>
public interface IAsyncRowSourceReader : IRecordCursor, IAsyncDisposable
{
    /// <summary>
    /// Asynchronously advances to the next row.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for aborting the operation.</param>
    /// <returns>True if there is another row; false if at end of data.</returns>
    ValueTask<bool> Read(CancellationToken cancellationToken = default);
}
