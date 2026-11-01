using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// Decides whether a quality check has a data set to read. The store a check reads is the one the data set is configured
/// over; it is never worked out from the data set's name.
/// </summary>
public static class QualityRuleTarget
{
    /// <summary>Succeeds when <paramref name="dataSetName"/> names a data set, and fails with the reason when it is empty.</summary>
    /// <param name="subject">What is being checked, for the message (for example the rule).</param>
    /// <param name="dataSetName">The data set name the rule or request carries.</param>
    /// <param name="logger">The endpoint's logger.</param>
    /// <returns>Success, or a failure that says nothing names a data set.</returns>
    public static IGenericResult RequireDataSet(string subject, string? dataSetName, ILogger logger) =>
        string.IsNullOrWhiteSpace(dataSetName)
            ? GenericResult.Failure(QualityCheckEndpointLog.NamesNoDataSet(logger, subject))
            : GenericResult.Success();

    /// <summary>Reads the rows a quality check runs over.</summary>
    /// <remarks>
    /// A read the gateway refuses or cannot make is returned as that failure (FDW-1162). Its code decides the status the
    /// caller is answered with and, for a refusal, the reason they are shown; a check never runs over no rows in its place,
    /// and never reports itself as a completed check that failed.
    /// </remarks>
    /// <param name="gateway">The gateway the data set is read through.</param>
    /// <param name="dataSetName">The data set whose rows are checked.</param>
    /// <param name="logger">The endpoint's logger.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The rows, or the gateway's own failure.</returns>
    public static async Task<IGenericResult<IReadOnlyList<Dictionary<string, object>>>> ReadRows(
        IDataGateway gateway, string dataSetName, ILogger logger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gateway);

        var read = await gateway.Execute<IEnumerable<Dictionary<string, object>>>(
            new QueryCommand<Dictionary<string, object>>(), new DataSetTarget(dataSetName), cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } rows)
        {
            QualityCheckEndpointLog.RowsNotRead(logger, dataSetName);
            return read.ToNewResult<IReadOnlyList<Dictionary<string, object>>>();
        }

        return GenericResult<IReadOnlyList<Dictionary<string, object>>>.Success(rows.ToList());
    }
}
