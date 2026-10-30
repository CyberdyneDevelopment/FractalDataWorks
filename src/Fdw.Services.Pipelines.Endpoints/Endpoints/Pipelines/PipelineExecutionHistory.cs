using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads the stored runs of <c>etl.PipelineExecution</c> a page at a time, newest first, and counts the runs a filter selects.
/// </summary>
/// <remarks>
/// The page and the count are asked of the database, so the cost of a history read is the rows it returns and not the size of the table.
/// </remarks>
internal static class PipelineExecutionHistory
{
    private const string CountColumn = "Total";

    private static DataStoreTarget Target(string connection) => new(connection, "etl", "PipelineExecution");

    /// <summary>Reads one page of the runs a filter selects, newest first.</summary>
    /// <param name="gateway">The gateway onto the operational store.</param>
    /// <param name="logger">The logger the endpoint reports through.</param>
    /// <param name="filter">Which runs, or nothing for every run.</param>
    /// <param name="skip">How many runs to skip.</param>
    /// <param name="take">How many runs to take.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page, or the failure of the read.</returns>
    internal static async Task<IGenericResult<IReadOnlyList<PipelineExecutionDbRecord>>> Page(
        IDataGateway gateway, ILogger logger, IFilterExpression? filter, int skip, int take, CancellationToken cancellationToken = default)
    {
        if (PipelineServiceTypes.OperationalConnection is not { Length: > 0 } connection)
            return GenericResult<IReadOnlyList<PipelineExecutionDbRecord>>.Failure(PipelineEndpointLog.HistoryConnectionNotConfigured(logger));

        var read = await gateway.Execute<IEnumerable<PipelineExecutionDbRecord>>(
            new QueryCommand<PipelineExecutionDbRecord>
            {
                Filter = filter,
                Ordering = new OrderingExpression
                {
                    OrderedFields =
                    [
                        new OrderedField { PropertyName = "StartedAt", Direction = SortDirections.ByName("Descending") },
                        new OrderedField { PropertyName = "Id", Direction = SortDirections.ByName("Descending") },
                    ],
                },
                Paging = new PagingExpression { Skip = skip, Take = take },
            },
            Target(connection), cancellationToken).ConfigureAwait(false);
        if (read.IsFailure) return read.ToNewResult<IReadOnlyList<PipelineExecutionDbRecord>>();

        return read.Value is null
            ? GenericResult<IReadOnlyList<PipelineExecutionDbRecord>>.Failure(PipelineEndpointLog.HistoryPageReturnedNoResult(logger, skip, take))
            : GenericResult<IReadOnlyList<PipelineExecutionDbRecord>>.Success(read.Value.ToList());
    }

    /// <summary>Counts the runs a filter selects.</summary>
    /// <param name="gateway">The gateway onto the operational store.</param>
    /// <param name="logger">The logger the endpoint reports through.</param>
    /// <param name="filter">Which runs, or nothing for every run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of runs, or the failure of the read.</returns>
    internal static async Task<IGenericResult<int>> Count(
        IDataGateway gateway, ILogger logger, IFilterExpression? filter, CancellationToken cancellationToken = default)
    {
        if (PipelineServiceTypes.OperationalConnection is not { Length: > 0 } connection)
            return GenericResult<int>.Failure(PipelineEndpointLog.HistoryConnectionNotConfigured(logger));

        var counted = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<Dictionary<string, object?>>
            {
                Filter = filter,
                Aggregation = new AggregationExpression
                {
                    GroupByFields = [],
                    Aggregations = new Dictionary<string, string>(StringComparer.Ordinal) { [CountColumn] = "COUNT(*)" },
                },
            },
            Target(connection), cancellationToken).ConfigureAwait(false);
        if (counted.IsFailure) return counted.ToNewResult<int>();

        return counted.Value?.FirstOrDefault() is { } row && row.TryGetValue(CountColumn, out var total) && total is IConvertible number
            ? GenericResult<int>.Success(checked((int)Convert.ToInt64(number, CultureInfo.InvariantCulture)))
            : GenericResult<int>.Failure(PipelineEndpointLog.HistoryCountReturnedNoNumber(logger));
    }
}
