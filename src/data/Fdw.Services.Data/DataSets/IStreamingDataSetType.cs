using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Execution;

namespace Fdw.Services.Data;

/// <summary>
/// Optional data set kind capability: read a data set as one forward stream when the connection under it can open a cursor (FDW-4911).
/// </summary>
/// <remarks>
/// A kind that reads one source (Simple) can hand its source's cursor on, shaping each batch of rows exactly as a page of the data set is
/// shaped (renames, field transformations, calculated fields). <see cref="StableRead"/> asks for it first and reads the data set in keyset
/// pages, unpaged or not at all when the kind answers that it cannot stream this data set.
/// </remarks>
public interface IStreamingDataSetType
{
    /// <summary>Opens the data set as one stream, or answers that it cannot be streamed.</summary>
    /// <param name="context">The data set, resolved and admitted, and what reaching its source needs.</param>
    /// <param name="command">The read: a query over the data set's field names, whose own paging is not used.</param>
    /// <param name="batchSize">How many rows are shaped together; at least one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The stream the caller disposes; <see langword="null"/> on success when this data set cannot be streamed (its source's connection opens
    /// no cursor, or the read aggregates); or why it failed.
    /// </returns>
    Task<IGenericResult<IRecordSource<DataRecord>?>> OpenStream(
        DataSetExecutionContext context, IDataCommand command, int batchSize, CancellationToken cancellationToken = default);
}
