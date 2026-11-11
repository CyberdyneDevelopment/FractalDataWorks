using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Reads every row of a data set for a table a Compute stage binds to it. The stage names a data set and nothing else: which store or
/// connection holds its rows is the data gateway's business.
/// </summary>
/// <remarks>
/// The rows are read through the gateway's stable read of a data set (FDW-4911): in keyset pages by the data set's key fields, or, for a
/// kind that builds its whole set in memory, once and unpaged under the gateway's configured row cap. A data set that can be read neither
/// way is refused. No row is repeated or skipped, and the page size is the gateway's configuration, not a number written here.
/// </remarks>
internal static class ComputeDataSetReader
{
    internal static async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> ReadAll(
        object? gatewayObject,
        string stageName,
        string tableName,
        string dataSet,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (gatewayObject is not IDataGateway gateway)
        {
            return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(ComputeSetLog.NoGateway(logger, stageName, dataSet));
        }

        var opened = await gateway.OpenRecordSource(new QueryCommand<Dictionary<string, object?>>(), new DataSetTarget(dataSet), cancellationToken).ConfigureAwait(false);
        if (opened.IsFailure || opened.Value is not { } source)
        {
            return Unreadable(opened, stageName, tableName, dataSet, logger);
        }

        await using (source.ConfigureAwait(false))
        {
            var rows = new List<IReadOnlyDictionary<string, object?>>();
            await foreach (var record in source.Read(cancellationToken).ConfigureAwait(false))
            {
                if (record.IsFailure)
                {
                    return Unreadable(record, stageName, tableName, dataSet, logger);
                }

                rows.Add(record.Value.ToDictionary());
            }

            return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows);
        }
    }

    private static IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Unreadable(
        IGenericResult cause, string stageName, string tableName, string dataSet, ILogger logger) =>
        GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(
            ComputeSetLog.DataSetUnreadable(logger, stageName, dataSet, tableName, FlowFailures.ReasonOf(cause) ?? string.Empty));
}
