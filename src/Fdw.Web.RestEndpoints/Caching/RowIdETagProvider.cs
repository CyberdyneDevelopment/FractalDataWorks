using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Caching;

/// <summary>
/// ETag provider that computes ETags from the most recent RowId in a container.
/// Uses the DataGateway to query the latest RowId (ordered descending, take 1)
/// and hashes it to produce a stable, quoted ETag string.
/// </summary>
public sealed class RowIdETagProvider : IETagProvider
{
    private readonly IDataGateway _dataGateway;
    private readonly ILogger<RowIdETagProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <c>RowIdETagProvider</c> class.
    /// </summary>
    /// <param name="dataGateway">The data gateway for executing queries.</param>
    /// <param name="logger">Optional logger for trace-level diagnostics.</param>
    public RowIdETagProvider(IDataGateway dataGateway, ILogger<RowIdETagProvider>? logger)
    {
        _dataGateway = dataGateway ?? throw new ArgumentNullException(nameof(dataGateway));
        _logger = logger ?? NullLogger<RowIdETagProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<string?> GetETag(string containerName, string connectionName, CancellationToken ct)
    {
        ETagLogger.ETagQueryStarted(_logger, containerName, connectionName);

        try
        {
            // Why: Addressing (DataStore/Container) was moved off IDataCommand onto DataStoreTarget
            // in the target-typed-gateway refactor. The caller passes connectionName which is the
            // DataStore name; path is null to search all paths in the store.
            var command = new QueryCommand<RowIdProjection>
            {
                Projection = new ProjectionExpression
                {
                    Fields = [new ProjectionField { PropertyName = "ModifyDate" }]
                },
                Ordering = new OrderingExpression
                {
                    OrderedFields =
                    [
                        new OrderedField
                        {
                            PropertyName = "ModifyDate",
                            Direction = SortDirections.ByName("Descending")
                        }
                    ]
                },
                Paging = new PagingExpression { Skip = 0, Take = 1 }
            };

            var result = await _dataGateway.Execute<System.Collections.Generic.IEnumerable<RowIdProjection>>(
                    command, new DataStoreTarget(connectionName, null, containerName), ct)
                .ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                ETagLogger.ETagQueryFailed(_logger, containerName, connectionName);
                return null;
            }

            var latest = result.Value.FirstOrDefault();
            if (latest is null || latest.ModifyDate == default)
            {
                ETagLogger.ETagNoRowId(_logger, containerName);
                return null;
            }

            var etag = ComputeETag(latest.ModifyDate);
            ETagLogger.ETagComputed(_logger, containerName, etag);
            return etag;
        }
        catch (Exception ex)
        {
            ETagLogger.ETagQueryError(_logger, ex, containerName, connectionName);
            return null;
        }
    }

    /// <summary>
    /// Computes a quoted ETag string from the latest ModifyDate using SHA256 truncation.
    /// </summary>
    // Why: RowId is DB-managed and invisible to the application, so the ETag is derived from the latest
    // ModifyDate in the container (the newest change marker) rather than the physical RowId.
    private static string ComputeETag(DateTimeOffset modifyDate)
    {
        var bytes = BitConverter.GetBytes(modifyDate.UtcTicks);
        var hash = SHA256.HashData(bytes);
        var hex = Convert.ToHexString(hash, 0, 8);
        return string.Concat("\"", hex.ToLower(CultureInfo.InvariantCulture), "\"");
    }

    /// <summary>
    /// Internal projection type for reading only the latest ModifyDate from a container.
    /// </summary>
    internal sealed class RowIdProjection
    {
        /// <summary>Gets or sets the row's last-modified timestamp (the ETag change marker).</summary>
        public DateTimeOffset ModifyDate { get; set; }
    }
}
