using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Graphs;

/// <summary>
/// Takes the batches a read of a Graph data set ends in and holds their rows (FDW-5291), refusing the batch that would take the read over its
/// row limit.
/// </summary>
/// <remarks>
/// The first batch that crosses the limit stops the run and is not held; <see cref="Failure"/> then holds the named failure the read answers,
/// with none of the rows held, so a part is never presented as the whole.
/// </remarks>
internal sealed class GraphReadCollector
{
    private readonly string _dataSet;
    private readonly int _maxRows;
    private readonly ILogger _logger;

    internal GraphReadCollector(string dataSet, int maxRows, ILogger logger)
    {
        _dataSet = dataSet;
        _maxRows = maxRows;
        _logger = logger;
    }

    /// <summary>Gets the rows held so far.</summary>
    internal List<IDictionary<string, object?>> Rows { get; } = [];

    /// <summary>Gets the row limit failure when a batch was refused for crossing the limit; null otherwise.</summary>
    internal IGenericResult? Failure { get; private set; }

    /// <summary>Takes one batch.</summary>
    /// <param name="batch">The batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the row limit failure that stops the run.</returns>
    internal Task<IGenericResult> Write(FlowBatch batch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Rows.Count + batch.Count > _maxRows)
        {
            var failure = GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetRowLimitExceeded"), _logger,
                ResultDetails.Create("dataSet", _dataSet, "limit", _maxRows));
            Failure = failure;
            return Task.FromResult(failure);
        }

        Rows.AddRange(batch.Rows);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
