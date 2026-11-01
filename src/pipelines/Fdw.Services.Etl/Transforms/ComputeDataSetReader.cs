using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Reads every row of a data set, page by page, for a table a Compute stage binds to it. The stage names a data set and nothing
/// else: which store or connection holds its rows is the data gateway's business.
/// </summary>
internal static class ComputeDataSetReader
{
    private const int PageSize = 1000;

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

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = new QueryCommand<Dictionary<string, object?>>
            {
                Paging = new PagingExpression { Skip = rows.Count, Take = PageSize },
            };
            var page = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(query, new DataSetTarget(dataSet), cancellationToken).ConfigureAwait(false);
            if (!page.IsSuccess || page.Value is null)
            {
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(
                    ComputeSetLog.DataSetUnreadable(logger, stageName, dataSet, tableName, string.Concat(page.CurrentMessage)));
            }

            var batch = page.Value.ToList();
            rows.AddRange(batch);
            if (batch.Count < PageSize)
            {
                return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows);
            }
        }
    }
}
